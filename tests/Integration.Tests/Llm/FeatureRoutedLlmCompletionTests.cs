using Application.Ai;
using FluentAssertions;
using Infrastructure.Ai;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Llm;

/// <summary>
/// The routing decision is what keeps the paid model off the highest-volume path (the 96-token
/// speaking tutor), so it is asserted per feature rather than only on the happy path.
/// </summary>
public class FeatureRoutedLlmCompletionTests
{
    private const string PremiumModel = "premium-model";
    private const string BudgetModel = "budget-model";

    private static FeatureRoutedLlmCompletion Create(params string[] premiumFeatures) =>
        new(
            new StubCompletion(PremiumModel),
            new StubCompletion(BudgetModel),
            new AiRoutingOptions { PremiumFeatures = string.Join(",", premiumFeatures) },
            NullLogger<FeatureRoutedLlmCompletion>.Instance);

    private static async Task<string?> CompleteAs(
        FeatureRoutedLlmCompletion router, AiFeature feature, Guid? learnerId = null)
    {
        using var scope = AiAdmissionContext.Push(
            new AiAdmissionContext.State(
                (learnerId ?? Guid.NewGuid()).ToString("N"), AiSubscriptionTier.Free, feature));
        return await router.CompleteAsync("system", "user", 96, CancellationToken.None);
    }

    [Theory]
    [InlineData(AiFeature.WritingAssessment)]
    [InlineData(AiFeature.Assistant)]
    public async Task Quality_critical_features_use_the_premium_model(AiFeature feature)
    {
        var router = Create(nameof(AiFeature.WritingAssessment), nameof(AiFeature.Assistant));

        (await CompleteAs(router, feature)).Should().Be(PremiumModel);
    }

    [Theory]
    [InlineData(AiFeature.SpeakingTutor)]
    [InlineData(AiFeature.SpeakingEvaluation)]
    [InlineData(AiFeature.Translation)]
    [InlineData(AiFeature.WritingGeneration)]
    [InlineData(AiFeature.VideoExplain)]
    [InlineData(AiFeature.Other)]
    public async Task High_volume_and_bulk_features_use_the_budget_model(AiFeature feature)
    {
        var router = Create(nameof(AiFeature.WritingAssessment), nameof(AiFeature.Assistant));

        (await CompleteAs(router, feature)).Should().Be(BudgetModel);
    }

    [Fact]
    public async Task Work_with_no_ai_scope_uses_the_budget_model()
    {
        // Background backfill and startup probes never push a scope. Defaulting them to the paid
        // model would silently put bulk content generation on the expensive path.
        var router = Create(nameof(AiFeature.WritingAssessment), nameof(AiFeature.Assistant));

        var result = await router.CompleteAsync("system", "user", 96, CancellationToken.None);

        result.Should().Be(BudgetModel);
    }

    [Fact]
    public void Unknown_feature_names_are_ignored_rather_than_crashing_startup()
    {
        var options = new AiRoutingOptions { PremiumFeatures = "WritingAssessment, NotAFeature" };

        options.ResolvePremiumFeatures().Should().BeEquivalentTo(new[] { AiFeature.WritingAssessment });
    }

    [Fact]
    public async Task An_empty_premium_set_routes_everything_to_the_budget_model()
    {
        var router = Create();

        (await CompleteAs(router, AiFeature.WritingAssessment)).Should().Be(BudgetModel);
    }

    /// <summary>Returns its own model id, so a test can see which branch was taken.</summary>
    private sealed class StubCompletion(string model) : IConversationLlmCompletion
    {
        public string Model { get; } = model;

        public Task<string?> CompleteAsync(
            string systemPrompt, string userPrompt, int maxOutputTokens, CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(Model);

        public Task<string?> CompleteConversationAsync(
            string systemPrompt,
            IReadOnlyList<(bool IsUser, string Text)> turns,
            int maxOutputTokens,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<string?>(Model);
    }
}
