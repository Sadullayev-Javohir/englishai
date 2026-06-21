using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Application.Developer.Ports;

namespace Infrastructure.Developer;

public sealed class HermesDeveloperAiGateway : IDeveloperAiGateway
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly DeveloperAiOptions _options;
    private int _nextKey = -1;

    public HermesDeveloperAiGateway(IHttpClientFactory httpClientFactory, DeveloperAiOptions options)
    {
        _httpClientFactory = httpClientFactory;
        _options = options;
    }

    public string Model => _options.Model;

    public async Task<DeveloperAiGatewayResponse> SendChatCompletionAsync(
        JsonElement request,
        CancellationToken cancellationToken)
    {
        var keys = _options.ResolvedApiKeys;
        if (keys.Count == 0)
            return JsonError(HttpStatusCode.ServiceUnavailable, "upstream_not_configured", "AI provider is not configured.");

        JsonObject payload;
        try
        {
            payload = JsonNode.Parse(request.GetRawText()) as JsonObject
                ?? throw new JsonException("Request body must be an object.");
        }
        catch (JsonException)
        {
            return JsonError(HttpStatusCode.BadRequest, "invalid_request", "Request body must be valid JSON.");
        }

        if (payload["messages"] is not JsonArray messages || messages.Count == 0)
            return JsonError(HttpStatusCode.BadRequest, "invalid_request", "messages must contain at least one item.");

        payload["model"] = Model;
        payload["max_tokens"] = ResolveMaxTokens(payload);
        payload.Remove("tools");
        payload.Remove("tool_choice");

        var start = Math.Abs(Interlocked.Increment(ref _nextKey));
        HttpResponseMessage? lastResponse = null;
        for (var attempt = 0; attempt < keys.Count; attempt++)
        {
            var key = keys[(start + attempt) % keys.Count];
            HttpResponseMessage response;
            try
            {
                response = await SendAsync(payload, key, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (OperationCanceledException)
            {
                lastResponse?.Dispose();
                return JsonError(HttpStatusCode.GatewayTimeout, "upstream_timeout", "AI provider request timed out.");
            }
            catch (HttpRequestException)
            {
                lastResponse?.Dispose();
                return JsonError(HttpStatusCode.BadGateway, "upstream_unavailable", "AI provider is temporarily unavailable.");
            }

            if (response.IsSuccessStatusCode)
            {
                lastResponse?.Dispose();
                return new DeveloperAiGatewayResponse(response);
            }

            if (!ShouldRotate(response.StatusCode) || attempt == keys.Count - 1)
            {
                lastResponse?.Dispose();
                return new DeveloperAiGatewayResponse(response);
            }

            lastResponse?.Dispose();
            lastResponse = response;
        }

        lastResponse?.Dispose();
        return JsonError(HttpStatusCode.BadGateway, "upstream_error", "AI provider request failed.");
    }

    private async Task<HttpResponseMessage> SendAsync(
        JsonObject payload,
        string apiKey,
        CancellationToken cancellationToken)
    {
        var baseUrl = _options.BaseUrl.TrimEnd('/');
        var http = _httpClientFactory.CreateClient("developer-ai");
        var message = new HttpRequestMessage(HttpMethod.Post, $"{baseUrl}/chat/completions")
        {
            Content = new StringContent(payload.ToJsonString(), Encoding.UTF8, "application/json"),
        };
        message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);
        message.Headers.TryAddWithoutValidation("HTTP-Referer", "https://englishai.uz");
        message.Headers.TryAddWithoutValidation("X-Title", "EnglishAI Developer API");
        return await http.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
    }

    private int ResolveMaxTokens(JsonObject payload)
    {
        var requested = payload["max_tokens"]?.GetValue<int?>()
                        ?? payload["max_completion_tokens"]?.GetValue<int?>()
                        ?? _options.MaxOutputTokens;
        payload.Remove("max_completion_tokens");
        return Math.Clamp(requested, 1, _options.MaxOutputTokens);
    }

    private static bool ShouldRotate(HttpStatusCode status) =>
        status is HttpStatusCode.Unauthorized
            or HttpStatusCode.Forbidden
            or HttpStatusCode.PaymentRequired
            or HttpStatusCode.TooManyRequests
        || (int)status >= 500;

    private static DeveloperAiGatewayResponse JsonError(HttpStatusCode status, string code, string message)
    {
        var json = JsonSerializer.Serialize(new { error = new { code, message } });
        return new DeveloperAiGatewayResponse(new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json"),
        });
    }
}
