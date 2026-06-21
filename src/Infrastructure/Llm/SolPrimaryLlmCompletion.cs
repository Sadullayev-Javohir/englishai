using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Llm;

public sealed class SolPrimaryLlmCompletion(
    IHttpClientFactory httpClientFactory,
    SolPrimaryOptions options,
    ILogger<SolPrimaryLlmCompletion> logger) : IConversationLlmCompletion
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    private readonly SemaphoreSlim _concurrency = new(Math.Clamp(options.MaxConcurrency, 1, 32));
    private readonly object _quotaLock = new();
    private int _remainingTokens = Math.Max(1, options.TokenLimitPerWindow);
    private DateTimeOffset _tokenResetAt = DateTimeOffset.UtcNow;

    public string Model => options.Model;

    public Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        CancellationToken cancellationToken = default) =>
        CompleteCoreAsync(
            [new("system", systemPrompt), new("user", userPrompt)],
            maxOutputTokens,
            cancellationToken);

    public Task<string?> CompleteConversationAsync(
        string systemPrompt,
        IReadOnlyList<(bool IsUser, string Text)> turns,
        int maxOutputTokens,
        CancellationToken cancellationToken = default)
    {
        var messages = new List<ChatMessage> { new("system", systemPrompt) };
        messages.AddRange(turns.Select(turn => new ChatMessage(turn.IsUser ? "user" : "assistant", turn.Text)));
        return CompleteCoreAsync(messages, maxOutputTokens, cancellationToken);
    }

    private async Task<string?> CompleteCoreAsync(
        IReadOnlyList<ChatMessage> messages,
        int maxOutputTokens,
        CancellationToken cancellationToken)
    {
        if (!options.IsConfigured) return null;

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(options.RequestTimeoutSeconds, 5, 120)));
        var permitAcquired = false;
        try
        {
            if (!await _concurrency.WaitAsync(0, timeout.Token))
            {
                logger.LogInformation("SOL concurrency is busy; using Hermes fallback immediately.");
                return null;
            }
            permitAcquired = true;
            if (!TryReserveProviderTokens())
            {
                logger.LogInformation("SOL token window is reserved; using Hermes fallback until {ResetAt}.", _tokenResetAt);
                return null;
            }

            var http = httpClientFactory.CreateClient("sol-primary");
            using var request = new HttpRequestMessage(HttpMethod.Post, CompletionUrl)
            {
                Content = JsonContent.Create(
                    new ChatRequest(
                        options.Model,
                        messages,
                        Math.Clamp(maxOutputTokens, 1, 4096),
                        string.IsNullOrWhiteSpace(options.ReasoningEffort) ? null : options.ReasoningEffort),
                    options: JsonOptions),
            };
            request.Headers.Add("api-key", options.ApiKey);

            using var response = await http.SendAsync(request, timeout.Token);
            UpdateQuota(response);
            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("SOL primary returned {StatusCode}; falling back to Hermes.", (int)response.StatusCode);
                return null;
            }

            var payload = await response.Content.ReadFromJsonAsync<ChatResponse>(JsonOptions, timeout.Token);
            return payload?.Choices?.Select(choice => choice.Message?.Content)
                .FirstOrDefault(content => !string.IsNullOrWhiteSpace(content))?.Trim();
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning("SOL primary timed out; falling back to Hermes.");
            return null;
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogWarning(exception, "SOL primary failed; falling back to Hermes.");
            return null;
        }
        finally
        {
            if (permitAcquired) _concurrency.Release();
        }
    }

    private bool TryReserveProviderTokens()
    {
        lock (_quotaLock)
        {
            var now = DateTimeOffset.UtcNow;
            if (now >= _tokenResetAt)
            {
                _remainingTokens = Math.Max(1, options.TokenLimitPerWindow);
                _tokenResetAt = now.AddSeconds(Math.Clamp(options.TokenWindowSeconds, 1, 600));
            }

            var reservation = Math.Clamp(options.TokenReservationPerRequest, 1, Math.Max(1, options.TokenLimitPerWindow));
            var safety = Math.Clamp(options.TokenSafetyReserve, 0, Math.Max(0, options.TokenLimitPerWindow - 1));
            if (_remainingTokens - reservation < safety) return false;
            _remainingTokens -= reservation;
            return true;
        }
    }

    private void UpdateQuota(HttpResponseMessage response)
    {
        lock (_quotaLock)
        {
            if (TryReadIntHeader(response, "x-ratelimit-remaining-tokens", out var remaining))
                _remainingTokens = Math.Max(0, remaining);

            if (TryReadIntHeader(response, "x-ratelimit-reset-tokens", out var resetSeconds))
                _tokenResetAt = DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(resetSeconds, 1, 600));
            else if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                _tokenResetAt = DateTimeOffset.UtcNow.AddSeconds(Math.Clamp(options.TokenWindowSeconds, 1, 600));

            if (response.StatusCode == System.Net.HttpStatusCode.TooManyRequests)
                _remainingTokens = 0;
        }
    }

    private static bool TryReadIntHeader(HttpResponseMessage response, string name, out int value)
    {
        value = 0;
        return response.Headers.TryGetValues(name, out var values)
               && int.TryParse(values.FirstOrDefault(), out value);
    }

    private string CompletionUrl => options.Endpoint.TrimEnd('/') + "/openai/v1/chat/completions";

    private sealed record ChatRequest(
        [property: JsonPropertyName("model")] string Model,
        [property: JsonPropertyName("messages")] IReadOnlyList<ChatMessage> Messages,
        [property: JsonPropertyName("max_completion_tokens")] int MaxCompletionTokens,
        [property: JsonPropertyName("reasoning_effort")] string? ReasoningEffort);

    private sealed record ChatMessage(
        [property: JsonPropertyName("role")] string Role,
        [property: JsonPropertyName("content")] string Content);

    private sealed record ChatResponse(
        [property: JsonPropertyName("choices")] IReadOnlyList<ChatChoice>? Choices);

    private sealed record ChatChoice(
        [property: JsonPropertyName("message")] ChatMessage? Message,
        [property: JsonPropertyName("finish_reason")] string? FinishReason);
}
