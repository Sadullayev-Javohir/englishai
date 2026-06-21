using Application.Ai;

namespace Infrastructure.Llm;

/// <summary>
/// Resolves the LLM the bulk content modules (vocabulary, reading, grammar, listening, books, video)
/// and the translators use. Every text-intelligence feature resolves the same
/// <see cref="ResilientHermesGatewayLlmCompletion"/> singleton, which owns the concurrency limit,
/// queue and circuit breaker; when the gateway has no key these modules fall back to the
/// deterministic Local stand-ins rather than fabricating content (docs/development-guide.md rules 8, 11).
///
/// Model SELECTION happens one layer down, in <see cref="FeatureRoutedLlmCompletion"/>, keyed on the
/// ambient <see cref="AiFeature"/> - deliberately not here, because routing above the resilient
/// wrapper would let a branch bypass admission control. Everything reached through this class runs
/// with <see cref="AiFeature.Other"/> unless the caller pushed a scope, i.e. on the budget model,
/// which is the intent for bulk content (docs/development-guide.md rule 10).
/// </summary>
public static class ContentLlm
{
    /// <summary>The model id the content modules are tagged with, for observability only.</summary>
    public static string Model(HermesGatewayOptions gateway) => gateway.Model;

    /// <summary>
    /// The completion the content modules run on. Only resolved when the gateway is configured;
    /// otherwise DI wires the Local generators instead - Claude is never reached.
    /// </summary>
    public static ILlmCompletion Completion(ILlmCompletion gatewayCompletion) => gatewayCompletion;
}
