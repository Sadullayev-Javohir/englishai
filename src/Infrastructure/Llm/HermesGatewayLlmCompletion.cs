using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Infrastructure.Llm;

/// <summary>
/// Shared OpenAI-compatible client for the self-hosted Hermes Agent Gateway.
/// Feature adapters own their prompts and parsing; this class owns endpoint, authentication,
/// model selection, retry and request transport for both single- and multi-turn calls.
/// </summary>
public sealed class HermesGatewayLlmCompletion : IConversationLlmCompletion
{
    private const string CompletionsPath = "/chat/completions";
    private static readonly IReadOnlyList<string> DefaultModelFallbacks = new[] { "hermes-agent" };

    private readonly OpenAiChatCompletionClient _client;
    private readonly HermesGatewayOptions _options;
    private readonly IReadOnlyList<string> _modelFallbacks;

    public HermesGatewayLlmCompletion(
        IHttpClientFactory httpClientFactory,
        HermesGatewayOptions options,
        ILogger<HermesGatewayLlmCompletion>? logger = null,
        IReadOnlyList<string>? modelFallbacks = null,
        int maxAttemptsPerModel = 3,
        int baseBackoffMs = 400)
    {
        _options = options;
        _modelFallbacks = modelFallbacks ?? DefaultModelFallbacks;
        _client = new OpenAiChatCompletionClient(
            httpClientFactory,
            "hermes-gateway",
            "HermesGateway",
            logger ?? (ILogger)NullLogger<HermesGatewayLlmCompletion>.Instance,
            maxAttemptsPerModel,
            baseBackoffMs);
    }

    public string Model => _options.Model;

    public Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        CancellationToken cancellationToken = default) =>
        CompleteCoreAsync(
            (model, apiKey) => _client.TryModelAsync(
                CompletionsUrl,
                apiKey,
                model,
                systemPrompt,
                userPrompt,
                BoundTokens(maxOutputTokens),
                _ => { },
                cancellationToken,
                excludeReasoning: true),
            cancellationToken);

    public Task<string?> CompleteConversationAsync(
        string systemPrompt,
        IReadOnlyList<(bool IsUser, string Text)> turns,
        int maxOutputTokens,
        CancellationToken cancellationToken = default) =>
        CompleteCoreAsync(
            (model, apiKey) => _client.TryModelAsync(
                CompletionsUrl,
                apiKey,
                model,
                systemPrompt,
                turns,
                BoundTokens(maxOutputTokens),
                _ => { },
                cancellationToken,
                excludeReasoning: true),
            cancellationToken);

    private async Task<string?> CompleteCoreAsync(
        Func<string, string, Task<string?>> complete,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var apiKey = _options.ResolvedApiKey;
        if (string.IsNullOrWhiteSpace(apiKey))
            return null;

        foreach (var model in ModelsToTry)
        {
            var result = await complete(model, apiKey);
            if (!string.IsNullOrWhiteSpace(result))
                return result;
        }

        return null;
    }

    private int BoundTokens(int requested) => Math.Clamp(requested, 1, _options.MaxOutputTokens);

    private IReadOnlyList<string> ModelsToTry
    {
        get
        {
            var primary = string.IsNullOrWhiteSpace(_options.Model) ? DefaultModelFallbacks[0] : _options.Model;
            return new[] { primary }
                .Concat(_modelFallbacks)
                .Where(model => !string.IsNullOrWhiteSpace(model))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }

    private string CompletionsUrl
    {
        get
        {
            var baseUrl = _options.Endpoint.TrimEnd('/');
            return baseUrl.EndsWith(CompletionsPath, StringComparison.OrdinalIgnoreCase)
                ? baseUrl
                : baseUrl + CompletionsPath;
        }
    }
}
