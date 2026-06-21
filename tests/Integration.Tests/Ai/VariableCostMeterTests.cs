using Application.Ai;
using FluentAssertions;
using Infrastructure.Ai;
using Infrastructure.Speaking;
using Xunit;

namespace Integration.Tests.Ai;

public sealed class VariableCostMeterTests
{
    [Fact]
    public void Snapshot_aggregates_categories_and_attributes_top_consumers()
    {
        var meter = CreateMeter(out _, dailyBudgetUsd: 10);

        meter.Record(VariableCostCategory.TextToSpeech, 100, "character", 0.25, "learner-b", AiFeature.SpeakingTutor, "/api/speaking/tts");
        meter.Record(VariableCostCategory.Ai, 300, "token", 0.75, "learner-a", AiFeature.Assistant, "/api/assistant/context");
        meter.Record(VariableCostCategory.Ai, 200, "token", 0.50, "learner-a", AiFeature.Assistant, "/api/assistant/context");

        var snapshot = meter.Snapshot();

        snapshot.DailyBudgetUsedUsd.Should().BeApproximately(1.50, 0.000001);
        snapshot.Categories.Should().ContainEquivalentOf(new VariableCostCategorySnapshot(
            VariableCostCategory.Ai, 2, 500, "token", 1.25));
        snapshot.TopCallers[0].Should().BeEquivalentTo(new VariableCostAttributionSnapshot("learner-a", 2, 1.25));
        snapshot.TopEndpoints[0].Should().BeEquivalentTo(new VariableCostAttributionSnapshot("/api/assistant/context", 2, 1.25));
    }

    [Fact]
    public void Snapshot_resets_local_totals_at_the_next_utc_day()
    {
        var meter = CreateMeter(out var clock);
        meter.Record(VariableCostCategory.Ai, 10, "token", 1);

        clock.Advance(TimeSpan.FromDays(1));

        var snapshot = meter.Snapshot();
        snapshot.Day.Should().Be(new DateOnly(2026, 8, 12));
        snapshot.DailyBudgetUsedUsd.Should().Be(0);
        snapshot.Categories.Should().BeEmpty();
    }

    [Fact]
    public void Exhausted_budget_blocks_free_but_preserves_pro_and_translation()
    {
        var meter = CreateMeter(out _, dailyBudgetUsd: 1);
        meter.Record(VariableCostCategory.SpeechToText, 1, "audio_hour", 1);

        var free = () => meter.EnsureAllowed(AiSubscriptionTier.Free, AiFeature.SpeakingEvaluation, "free");
        free.Should().Throw<AiAdmissionException>().Which.Code.Should().Be("budget_exhausted");
        meter.Invoking(value => value.EnsureAllowed(AiSubscriptionTier.Pro, AiFeature.SpeakingEvaluation, "pro"))
            .Should().NotThrow();
        meter.Invoking(value => value.EnsureAllowed(AiSubscriptionTier.Free, AiFeature.Translation, "translation"))
            .Should().NotThrow();
    }

    [Fact]
    public async Task Admin_override_replaces_the_configured_daily_budget()
    {
        var meter = CreateMeter(out _, dailyBudgetUsd: 25);

        await meter.SetDailyBudgetAsync(50);

        meter.Snapshot().DailyBudgetLimitUsd.Should().Be(50);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(10000.01)]
    public async Task Admin_override_rejects_out_of_range_values(double budget)
    {
        var meter = CreateMeter(out _);

        var action = () => meter.SetDailyBudgetAsync(budget);

        await action.Should().ThrowAsync<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Pricing_is_configured_only_when_every_enabled_provider_has_rates()
    {
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero));
        var ai = new AiAdmissionOptions { PricingConfigured = true };
        var speech = new AzureSpeechOptions { Key = "configured", Region = "eastus", PricingConfigured = false };
        var meter = new VariableCostMeter(ai, clock, speech: speech);

        meter.Snapshot().PricingConfigured.Should().BeFalse();
        speech.PricingConfigured = true;
        meter.Snapshot().PricingConfigured.Should().BeTrue();
        ai.PricingConfigured = false;
        meter.Snapshot().PricingConfigured.Should().BeFalse();
    }

    [Fact]
    public void Production_default_rates_mark_ai_and_speech_pricing_as_configured()
    {
        var ai = new AiAdmissionOptions { PricingConfigured = true };
        var speech = new AzureSpeechOptions
        {
            PricingConfigured = true,
            SpeechToTextCostPerAudioHourUsd = 1.32,
            TextToSpeechCostPerMillionCharactersUsd = 16,
            PronunciationCostPerAudioHourUsd = 1.32,
        };
        var meter = new VariableCostMeter(
            ai,
            new MutableTimeProvider(new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero)),
            speech: speech);

        meter.Snapshot().PricingConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task Admission_records_token_cost_with_request_attribution()
    {
        var options = new AiAdmissionOptions
        {
            PricingConfigured = true,
            DailyBudgetUsd = 10,
            EstimatedInputCostPerMillionTokensUsd = 2,
            EstimatedOutputCostPerMillionTokensUsd = 4,
        };
        var clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero));
        var meter = new VariableCostMeter(options, clock);
        var admission = new AiAdmissionControl(options, clock, meter);
        using var context = AiAdmissionContext.Push(new(
            "learner-1",
            AiSubscriptionTier.Free,
            AiFeature.Assistant,
            "/api/assistant/context/stream"));

        await using (var lease = await admission.AcquireAsync(
                         new AiAdmissionRequest("learner-1", AiSubscriptionTier.Free, AiFeature.Assistant, 1_000_000, 1_000_000),
                         CancellationToken.None))
        {
            lease.Complete(500_000);
        }

        var snapshot = meter.Snapshot();
        snapshot.DailyBudgetUsedUsd.Should().BeApproximately(4, 0.000001);
        snapshot.TopCallers.Single().Key.Should().Be("learner-1");
        snapshot.TopEndpoints.Single().Key.Should().Be("/api/assistant/context/stream");
        admission.Snapshot().EstimatedCostUsd.Should().BeApproximately(4, 0.000001);
    }

    [Fact]
    public void RecordAdjustment_subtracts_without_counting_another_request()
    {
        var meter = CreateMeter(out _, dailyBudgetUsd: 10);
        meter.Record(VariableCostCategory.VoiceLive, 2 / 60d, "audio_hour", 1.00, "learner-a", AiFeature.SpeakingTutor, "/api/speaking/vl");

        // A 30-second session against a 2-minute reservation gives most of the money back.
        meter.RecordAdjustment(VariableCostCategory.VoiceLive, -1.5 / 60d, "audio_hour", -0.75, "learner-a", AiFeature.SpeakingTutor, "/api/speaking/vl");

        var snapshot = meter.Snapshot();
        snapshot.DailyBudgetUsedUsd.Should().BeApproximately(0.25, 0.000001);
        var category = snapshot.Categories.Single(item => item.Category == VariableCostCategory.VoiceLive);
        category.Requests.Should().Be(1, "settling a session is not a second request");
        category.Units.Should().BeApproximately(0.5 / 60d, 0.000001);
        snapshot.TopCallers.Single().Requests.Should().Be(1);
    }

    [Fact]
    public void RecordAdjustment_ignores_non_finite_and_empty_deltas()
    {
        var meter = CreateMeter(out _);
        meter.Record(VariableCostCategory.VoiceLive, 1, "audio_hour", 1);

        meter.RecordAdjustment(VariableCostCategory.VoiceLive, double.NaN, "audio_hour", -1);
        meter.RecordAdjustment(VariableCostCategory.VoiceLive, -1, "audio_hour", double.PositiveInfinity);
        meter.RecordAdjustment(VariableCostCategory.VoiceLive, 0, "audio_hour", 0);

        meter.Snapshot().DailyBudgetUsedUsd.Should().BeApproximately(1, 0.000001);
    }

    [Fact]
    public void Budget_never_reports_negative_spend()
    {
        var meter = CreateMeter(out _);
        meter.Record(VariableCostCategory.VoiceLive, 1, "audio_hour", 1);

        meter.RecordAdjustment(VariableCostCategory.VoiceLive, -5, "audio_hour", -5);

        meter.Snapshot().DailyBudgetUsedUsd.Should().Be(0);
    }

    [Fact]
    public void One_heavy_account_is_capped_before_it_can_shed_everyone_else()
    {
        // The global budget is all-or-nothing, so without a per-learner ceiling a single runaway
        // account drains it and every other free learner is blocked for the rest of the day.
        var meter = CreateMeter(out _, dailyBudgetUsd: 100, freeDailyBudgetPerLearnerUsd: 0.50);

        meter.Record(VariableCostCategory.Ai, 1000, "token", 0.60, "heavy-learner", AiFeature.Assistant);

        var heavy = () => meter.EnsureAllowed(AiSubscriptionTier.Free, AiFeature.Assistant, "heavy-learner");
        heavy.Should().Throw<AiAdmissionException>()
            .Which.Code.Should().Be("learner_budget_exhausted");

        // Everyone else is untouched: the global budget is nowhere near exhausted.
        var other = () => meter.EnsureAllowed(AiSubscriptionTier.Free, AiFeature.Assistant, "quiet-learner");
        other.Should().NotThrow();
    }

    [Fact]
    public void A_paying_account_is_capped_too_just_far_higher()
    {
        var meter = CreateMeter(
            out _, dailyBudgetUsd: 100, freeDailyBudgetPerLearnerUsd: 0.50, proDailyBudgetPerLearnerUsd: 2);

        meter.Record(VariableCostCategory.Ai, 1000, "token", 1, "pro-learner", AiFeature.Assistant);

        // Still under the Pro ceiling, and Pro is exempt from the global shed.
        var underCeiling = () => meter.EnsureAllowed(AiSubscriptionTier.Pro, AiFeature.Assistant, "pro-learner");
        underCeiling.Should().NotThrow();

        meter.Record(VariableCostCategory.Ai, 1000, "token", 1.5, "pro-learner", AiFeature.Assistant);

        var overCeiling = () => meter.EnsureAllowed(AiSubscriptionTier.Pro, AiFeature.Assistant, "pro-learner");
        overCeiling.Should().Throw<AiAdmissionException>()
            .Which.Code.Should().Be("learner_budget_exhausted");
    }

    [Fact]
    public void Translation_stays_reachable_even_for_an_exhausted_account()
    {
        // Shedding translation strands a learner mid-lesson on a word they cannot read, and it is
        // the cheapest call in the app.
        var meter = CreateMeter(out _, dailyBudgetUsd: 100, freeDailyBudgetPerLearnerUsd: 0.10);

        meter.Record(VariableCostCategory.Ai, 1000, "token", 5, "heavy-learner", AiFeature.Assistant);

        var act = () => meter.EnsureAllowed(AiSubscriptionTier.Free, AiFeature.Translation, "heavy-learner");
        act.Should().NotThrow();
    }

    [Fact]
    public void A_zero_per_learner_ceiling_disables_the_check()
    {
        var meter = CreateMeter(out _, dailyBudgetUsd: 100, freeDailyBudgetPerLearnerUsd: 0);

        meter.Record(VariableCostCategory.Ai, 1000, "token", 50, "heavy-learner", AiFeature.Assistant);

        var act = () => meter.EnsureAllowed(AiSubscriptionTier.Free, AiFeature.Assistant, "heavy-learner");
        act.Should().NotThrow();
    }

    private static VariableCostMeter CreateMeter(
        out MutableTimeProvider clock,
        double dailyBudgetUsd = 25,
        double freeDailyBudgetPerLearnerUsd = 0,
        double proDailyBudgetPerLearnerUsd = 0)
    {
        clock = new MutableTimeProvider(new DateTimeOffset(2026, 8, 11, 10, 0, 0, TimeSpan.Zero));
        return new VariableCostMeter(
            new AiAdmissionOptions
            {
                DailyBudgetUsd = dailyBudgetUsd,
                PricingConfigured = true,
                // Off unless a test opts in, so the existing global-budget cases keep asserting the
                // global behaviour rather than tripping the per-learner ceiling first.
                FreeDailyBudgetPerLearnerUsd = freeDailyBudgetPerLearnerUsd,
                ProDailyBudgetPerLearnerUsd = proDailyBudgetPerLearnerUsd,
            },
            clock);
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
