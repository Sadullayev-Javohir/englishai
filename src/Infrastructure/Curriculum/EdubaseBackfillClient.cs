using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Curriculum;

public sealed class EdubaseBackfillClient
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };
    private readonly IHttpClientFactory _factory;
    private readonly BackfillLlmOptions _options;
    private readonly ILogger<EdubaseBackfillClient> _logger;

    public EdubaseBackfillClient(IHttpClientFactory factory, BackfillLlmOptions options,
        ILogger<EdubaseBackfillClient> logger)
    { _factory = factory; _options = options; _logger = logger; }

    public string Model => _options.ResolvedModel;

    public async Task<string?> CompleteAsync(string systemPrompt, string userPrompt, string idempotencyKey,
        int? maxTokens = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(_options.ResolvedApiKey))
            throw new InvalidOperationException("BACKFILL_OPENAI_API_KEY is required.");

        var models = new[] { _options.ResolvedModel, "gpt-5.6-terra" }
            .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        foreach (var model in models)
        {
            for (var attempt = 1; attempt <= _options.MaxAttempts; attempt++)
            {
                using var request = new HttpRequestMessage(HttpMethod.Post,
                    _options.ResolvedBaseUrl.TrimEnd('/') + "/chat/completions");
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _options.ResolvedApiKey);
                request.Headers.TryAddWithoutValidation("Idempotency-Key", idempotencyKey);
                request.Content = JsonContent.Create(new Request(model,
                    new[] { new Message("system", systemPrompt), new Message("user", userPrompt) },
                    Math.Clamp(maxTokens ?? _options.MaxOutputTokens, 1024, _options.MaxOutputTokens)), options: Json);
                using var response = await _factory.CreateClient("backfill-edubase").SendAsync(request, cancellationToken);
                var payload = await response.Content.ReadAsStringAsync(cancellationToken);
                if (response.IsSuccessStatusCode)
                {
                    try
                    {
                        var parsed = JsonSerializer.Deserialize<Response>(payload, Json);
                        var content = parsed?.Choices?.FirstOrDefault()?.Message?.Content;
                        if (!string.IsNullOrWhiteSpace(content)) return content;
                    }
                    catch (JsonException) { }
                }

                var transient = response.StatusCode == HttpStatusCode.TooManyRequests || (int)response.StatusCode >= 500 ||
                    payload.Contains("so'rov limiti", StringComparison.OrdinalIgnoreCase) ||
                    payload.Contains("so‘rov limiti", StringComparison.OrdinalIgnoreCase);
                _logger.LogWarning("Edubase backfill call failed for {Model}, attempt {Attempt}: {Status}",
                    model, attempt, (int)response.StatusCode);
                if (!transient || attempt == _options.MaxAttempts) break;
                var jitter = Random.Shared.NextDouble() * 2.5;
                await Task.Delay(TimeSpan.FromSeconds(Math.Min(90, Math.Pow(2, attempt) * 2 + jitter)), cancellationToken);
            }
        }
        return null;
    }

    private sealed record Request(string Model, IReadOnlyList<Message> Messages,
        [property: JsonPropertyName("max_tokens")] int MaxTokens);
    private sealed record Message(string Role, string Content);
    private sealed record Response(IReadOnlyList<Choice>? Choices);
    private sealed record Choice(Message? Message, [property: JsonPropertyName("finish_reason")] string? FinishReason);
}
