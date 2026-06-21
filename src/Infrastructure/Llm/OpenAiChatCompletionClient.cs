using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Llm;

/// <summary>
/// HTTP mechanics for OpenAI-compatible chat-completions APIs. Handles per-model retry with
/// exponential backoff on transient failures (429/5xx). The caller owns its model fallback list and
/// API key resolution and calls <see cref="TryModelAsync"/> once per model, walking its chain until
/// one succeeds.
///
/// <see cref="HermesGatewayLlmCompletion"/> is the only caller left: the third-party providers that
/// also spoke this wire format (Groq, OpenRouter, Google AI Studio) were removed on 2026-07-28 when
/// the self-hosted gateway became the sole LLM backend. The class stays separate from that adapter so
/// transport concerns (retry, rate-limit handling, response parsing) stay out of the prompt-owning
/// adapter (docs/development-guide.md rule 9).
/// </summary>
internal sealed class OpenAiChatCompletionClient
{
    // Cap how much of an error body we log so a verbose provider error page can't flood the logs.
    private const int MaxErrorBodyChars = 500;

    // Per-model transient-failure retry budget defaults. 5 attempts with exponential backoff
    // (starting 2s) rides out 429/5xx spikes without hard-stopping; after the budget the model falls
    // through to the next in the caller's chain. Kept generous in production so rate-limits slow us
    // (never fail us). Overridable per instance so tests can shrink both to keep real Task.Delay calls
    // out of the fast path.
    private const int DefaultMaxAttemptsPerModel = 5;
    private const int DefaultBaseBackoffMs = 2000;

    // Token budget floor for reasoning models (see the call site in TryModelAsync). Sized so the model
    // can finish its internal trace AND still emit the answer: truncating mid-trace wastes the whole
    // budget for nothing, so a cap that is too small is strictly more expensive than one that is
    // generous. A larger cap costs nothing on its own - only tokens the model actually generates are
    // billed, and the trace length is chosen by the model, not by this ceiling.
    private const int MinimumReasoningModelBudget = 1024;

    // A server-provided Retry-After longer than this is treated as "this window won't reopen soon" (a
    // daily/hourly cap), not a brief burst worth waiting out - see the 429 handling below. Comfortably
    // above the ~32s our own exponential backoff ever produces (5 attempts, 2s base), so it only ever
    // triggers on a REAL server-provided Retry-After, never on our own synthetic delay.
    private static readonly TimeSpan MaxAcceptableRetryDelay = TimeSpan.FromSeconds(60);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly string _httpClientName;
    private readonly string _providerName;
    private readonly ILogger _logger;
    private readonly int _maxAttemptsPerModel;
    private readonly int _baseBackoffMs;

    public OpenAiChatCompletionClient(
        IHttpClientFactory httpClientFactory, string httpClientName, string providerName, ILogger logger,
        int maxAttemptsPerModel = DefaultMaxAttemptsPerModel, int baseBackoffMs = DefaultBaseBackoffMs)
    {
        _httpClientFactory = httpClientFactory;
        _httpClientName = httpClientName;
        _providerName = providerName;
        _logger = logger;
        _maxAttemptsPerModel = maxAttemptsPerModel;
        _baseBackoffMs = baseBackoffMs;
    }

    /// <summary>
    /// Runs one model of the caller's fallback chain to completion (including its own transient-retry
    /// budget), returning the reply text or null if this model could not produce one. A fresh
    /// <see cref="HttpClient"/> is taken from the factory per call (no captive handler in the caller's
    /// singleton).
    /// </summary>
    public Task<string?> TryModelAsync(
        string completionsUrl, string apiKey, string model, string systemPrompt, string userPrompt,
        int maxOutputTokens, Action<HttpRequestMessage> configureHeaders, CancellationToken cancellationToken,
        bool excludeReasoning = false)
        => TryModelAsync(
            completionsUrl, apiKey, model,
            new[] { new ChatMessage("system", systemPrompt), new ChatMessage("user", userPrompt) },
            maxOutputTokens, configureHeaders, cancellationToken, excludeReasoning);

    /// <summary>
    /// Multi-turn variant for callers that need full conversation history (e.g. the Speaking tutor).
    /// <paramref name="turns"/> alternates learner/assistant turns following the system prompt.
    /// </summary>
    public Task<string?> TryModelAsync(
        string completionsUrl, string apiKey, string model, string systemPrompt,
        IReadOnlyList<(bool IsUser, string Text)> turns,
        int maxOutputTokens, Action<HttpRequestMessage> configureHeaders, CancellationToken cancellationToken,
        bool excludeReasoning = false)
    {
        var messages = new List<ChatMessage> { new("system", systemPrompt) };
        messages.AddRange(turns.Select(t => new ChatMessage(t.IsUser ? "user" : "assistant", t.Text)));
        return TryModelAsync(
            completionsUrl, apiKey, model, messages, maxOutputTokens, configureHeaders, cancellationToken,
            excludeReasoning);
    }

    private async Task<string?> TryModelAsync(
        string completionsUrl, string apiKey, string model, IReadOnlyList<ChatMessage> messages,
        int maxOutputTokens, Action<HttpRequestMessage> configureHeaders, CancellationToken cancellationToken,
        bool excludeReasoning)
    {
        try
        {
            // Reasoning models spend max_tokens on their internal chain-of-thought BEFORE emitting a
            // single visible token, so a cap sized for the answer alone is spent entirely on thinking:
            // the call comes back finish_reason "length" with empty content, every time, deterministically.
            // Verified live twice - on the gateway's StepFun model at an 8-128 token cap (2026-07-22) and
            // on gpt-oss at the video explain-chat's 220-token cap (2026-07-28, before those providers
            // were removed). Callers that ask for reasoning exclusion get the floor; ordinary callers
            // keep their own cost cap (docs/development-guide.md rule 10), where the floor buys nothing.
            var effectiveMaxOutputTokens = excludeReasoning
                ? Math.Max(maxOutputTokens, MinimumReasoningModelBudget)
                : maxOutputTokens;
            var request = new ChatCompletionRequest(
                model, messages, effectiveMaxOutputTokens, excludeReasoning ? new ReasoningConfig(true) : null);

            var http = _httpClientFactory.CreateClient(_httpClientName);

            for (var attempt = 1; ; attempt++)
            {
                using var requestMessage = new HttpRequestMessage(HttpMethod.Post, completionsUrl)
                {
                    Content = JsonContent.Create(request, options: JsonOptions),
                };
                requestMessage.Headers.Authorization =
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", apiKey);
                configureHeaders(requestMessage);

                using var response = await http.SendAsync(requestMessage, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await SafeReadBodyAsync(response, cancellationToken);

                    // A 429 reporting zero quota remaining means this model's window (often a whole-day
                    // cap) is exhausted, not a brief burst - retrying with backoff would just wait out a
                    // window that won't reopen for minutes/hours. Skip straight to the next model.
                    //
                    // The self-hosted gateway does not rate-limit this way, so in practice this branch
                    // no longer fires; it is kept because it is generic OpenAI-compatible transport
                    // handling and costs nothing when unused. Both header spellings are still checked:
                    // the "zero remaining" signal was spelled as the bare "X-RateLimit-Remaining" by
                    // some providers and split into "-Requests" / "-Tokens" by others. Verified live
                    // 2026-07-19, the split headers could be scoped to a PER-MINUTE window while the
                    // 429 body said the PER-DAY cap was exhausted - so they read as a healthy nonzero
                    // "remaining" and the header check alone missed the daily case. The
                    // MaxAcceptableRetryDelay check below is the real safety net for that: any
                    // server-provided Retry-After longer than a brief burst (33-53 MINUTES was observed)
                    // means the window isn't reopening soon regardless of what the headers claim.
                    var retryAfter = response.Headers.RetryAfter?.Delta;
                    if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests
                        && (IsQuotaExhausted(response) || retryAfter > MaxAcceptableRetryDelay))
                    {
                        _logger.LogWarning(
                            "{Provider} call failed (model {Model}): 429 with quota exhausted for this window " +
                            "(Retry-After: {RetryAfter}). Moving to next model immediately. Body: {Body}",
                            _providerName, model, retryAfter, body);
                        return null;
                    }

                    // 429 / 5xx on this model → retry a few times, then fall through to next model.
                    if (IsTransient(response.StatusCode) && attempt < _maxAttemptsPerModel)
                    {
                        var delay = retryAfter
                                    ?? TimeSpan.FromMilliseconds(_baseBackoffMs * Math.Pow(2, attempt - 1));
                        _logger.LogWarning(
                            "{Provider} call failed (model {Model}, attempt {Attempt}/{Max}): {StatusCode}. Retrying in {DelayMs}ms. Body: {Body}",
                            _providerName, model, attempt, _maxAttemptsPerModel, (int)response.StatusCode, (int)delay.TotalMilliseconds, body);
                        await Task.Delay(delay, cancellationToken);
                        continue;
                    }

                    // Non-transient, or retries exhausted on this model → stop trying THIS model.
                    _logger.LogWarning(
                        "{Provider} call failed (model {Model}): {StatusCode} {Reason}. Moving to next model. Body: {Body}",
                        _providerName, model, (int)response.StatusCode, response.ReasonPhrase, body);
                    return null;
                }

                var payload = await response.Content.ReadFromJsonAsync<ChatCompletionResponse>(JsonOptions, cancellationToken);
                if (payload?.Choices?.Any(choice =>
                        string.Equals(choice.FinishReason, "error", StringComparison.OrdinalIgnoreCase)) == true)
                {
                    _logger.LogWarning(
                        "{Provider} returned an error completion (model {Model}). Moving to the next provider.",
                        _providerName, model);
                    return null;
                }
                var text = payload?.Choices?
                    .Select(c => c.Message?.Content)
                    .Where(t => !string.IsNullOrEmpty(t));

                var joined = text is null ? null : string.Concat(text);
                if (string.IsNullOrWhiteSpace(joined))
                {
                    var finishReason = payload?.Choices?.FirstOrDefault()?.FinishReason ?? "none";
                    // "length" with no content means the whole budget went to something invisible - an
                    // unrecognised reasoning model's trace. Name that explicitly so the fix (add the id's
                    // marker to ReasoningModelMarkers) is obvious from the admin log alone.
                    if (string.Equals(finishReason, "length", StringComparison.OrdinalIgnoreCase))
                        _logger.LogWarning(
                            "{Provider} call returned no text (model {Model}): the {MaxTokens}-token budget was " +
                            "consumed before any visible token, which means the model's reasoning trace did not " +
                            "fit. Raise MinimumReasoningModelBudget or HermesGateway:MaxOutputTokens.",
                            _providerName, model, effectiveMaxOutputTokens);
                    else
                        _logger.LogWarning(
                            "{Provider} call returned no text (model {Model}, finishReason {FinishReason}).",
                            _providerName, model, finishReason);
                    return null;
                }

                return joined;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "{Provider} call threw (model {Model}).", _providerName, model);
            return null;
        }
    }

    private static bool IsTransient(System.Net.HttpStatusCode status) =>
        status == System.Net.HttpStatusCode.TooManyRequests || (int)status >= 500;

    private static readonly string[] ZeroRemainingHeaderNames =
    {
        "X-RateLimit-Remaining",
        "X-RateLimit-Remaining-Requests",
        "X-RateLimit-Remaining-Tokens",
    };

    private static bool IsQuotaExhausted(HttpResponseMessage response) =>
        ZeroRemainingHeaderNames.Any(name =>
            response.Headers.TryGetValues(name, out var values) && values.FirstOrDefault() == "0");

    private static async Task<string> SafeReadBodyAsync(
        HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            return body.Length <= MaxErrorBodyChars ? body : body[..MaxErrorBodyChars] + "…";
        }
        catch
        {
            return "<unreadable>";
        }
    }

    // Request shape (camelCased by JsonOptions): OpenAI-compatible chat completions.
    private sealed record ChatCompletionRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens,
        [property: JsonPropertyName("reasoning")] ReasoningConfig? Reasoning = null);

    // OpenRouter-style field some gateways honour to suppress reasoning-model chain-of-thought from
    // eating the whole output-token budget (see the caller-facing note above TryModelAsync).
    private sealed record ReasoningConfig(
        [property: JsonPropertyName("exclude")] bool Exclude);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    // Response shape.
    private sealed record ChatCompletionResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatCompletionChoice>? Choices);

    private sealed record ChatCompletionChoice(
        [property: JsonPropertyName("message")] ChatMessage? Message,
        [property: JsonPropertyName("finish_reason")] string? FinishReason = null);
}
