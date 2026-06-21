using Application.Ai;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Llm;

/// <summary>
/// Sends each completion to the model its feature warrants (see <see cref="AiRoutingOptions"/>).
///
/// This sits UNDERNEATH <see cref="ResilientHermesGatewayLlmCompletion"/>, not above it: that wrapper
/// owns the concurrency limit, the queue and the circuit breaker, so routing outside it would let one
/// branch bypass admission control entirely.
///
/// The feature comes from the ambient <see cref="AiAdmissionContext"/>, which every learner-facing
/// call already establishes via <c>IAiFeatureScope</c>. Work with no scope (background backfill,
/// startup probes) resolves <see cref="AiFeature.Other"/> and therefore takes the budget model -
/// the correct default, since bulk content generation is exactly what must not run on the paid model.
/// </summary>
public sealed class FeatureRoutedLlmCompletion : IConversationLlmCompletion
{
    private readonly IConversationLlmCompletion _premium;
    private readonly IConversationLlmCompletion _budget;
    private readonly IReadOnlySet<AiFeature> _premiumFeatures;
    private readonly ILogger<FeatureRoutedLlmCompletion> _logger;

    public FeatureRoutedLlmCompletion(
        IConversationLlmCompletion premium,
        IConversationLlmCompletion budget,
        AiRoutingOptions options,
        ILogger<FeatureRoutedLlmCompletion> logger)
    {
        _premium = premium;
        _budget = budget;
        _premiumFeatures = options.ResolvePremiumFeatures();
        _logger = logger;
        _logger.LogInformation(
            "LLM routing: {Premium} for {Features}; {Budget} for everything else.",
            premium.Model,
            _premiumFeatures.Count == 0 ? "(none)" : string.Join(", ", _premiumFeatures),
            budget.Model);
    }

    /// <summary>The model most traffic runs on - this is an observability tag, not a routing input.</summary>
    public string Model => _budget.Model;

    public Task<string?> CompleteAsync(
        string systemPrompt,
        string userPrompt,
        int maxOutputTokens,
        CancellationToken cancellationToken = default) =>
        Route().CompleteAsync(systemPrompt, userPrompt, maxOutputTokens, cancellationToken);

    public Task<string?> CompleteConversationAsync(
        string systemPrompt,
        IReadOnlyList<(bool IsUser, string Text)> turns,
        int maxOutputTokens,
        CancellationToken cancellationToken = default) =>
        Route().CompleteConversationAsync(systemPrompt, turns, maxOutputTokens, cancellationToken);

    internal IConversationLlmCompletion Route()
    {
        var feature = AiAdmissionContext.Current.Feature;
        return _premiumFeatures.Contains(feature) ? _premium : _budget;
    }
}
