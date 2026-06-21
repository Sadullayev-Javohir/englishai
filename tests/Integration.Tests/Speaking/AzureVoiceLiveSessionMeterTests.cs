using Application.Ai;
using FluentAssertions;
using Infrastructure.Ai;
using Infrastructure.Speaking;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// Voice Live is the one Azure surface the server cannot observe directly — the browser streams
/// audio straight to Azure — so every guard lives at token mint and at the completion report.
/// These tests pin that contract: what gets reserved, what refuses, and what a hostile or broken
/// client can talk the server into billing.
/// </summary>
public sealed class AzureVoiceLiveSessionMeterTests
{
    private const string Caller = "learner-a";
    private static readonly DateTimeOffset Start = new(2026, 8, 15, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Reserve_books_the_up_front_charge_and_returns_the_session_ceiling()
    {
        var (meter, costs, _) = CreateMeter();
        using var _scope = PushCaller();

        var ticket = await meter.ReserveAsync("british");

        ticket.SessionId.Should().HaveLength(32);
        ticket.MaxSessionSeconds.Should().Be(600);
        ticket.RemainingDailyMinutes.Should().Be(28);

        var category = costs.Snapshot().Categories.Single();
        category.Category.Should().Be(VariableCostCategory.VoiceLive);
        category.Requests.Should().Be(1);
        // 2 reserved minutes at $30/audio-hour = $1.00.
        category.EstimatedCostUsd.Should().BeApproximately(1.00, 0.000001);
    }

    [Fact]
    public async Task Reserve_refuses_once_the_platform_budget_is_exhausted()
    {
        var (meter, costs, _) = CreateMeter(dailyBudgetUsd: 1);
        costs.Record(VariableCostCategory.Ai, 1, "token", 1);
        using var _scope = PushCaller();

        var act = () => meter.ReserveAsync("british");

        (await act.Should().ThrowAsync<AiAdmissionException>()).Which.Code.Should().Be("budget_exhausted");
    }

    [Fact]
    public async Task Reserve_refuses_once_the_learner_has_used_their_daily_allowance()
    {
        // 6 minutes a day at 2 minutes reserved per session = exactly three sessions.
        var (meter, _, _) = CreateMeter(dailyMinutesPerLearner: 6);
        using var _scope = PushCaller();

        for (var i = 0; i < 3; i++)
            await meter.ReserveAsync("british");

        var act = () => meter.ReserveAsync("british");

        var exception = (await act.Should().ThrowAsync<AiAdmissionException>()).Which;
        exception.Code.Should().Be("voice_live_daily_limit");
        exception.StatusCode.Should().Be(429);
        exception.RetryAfterSeconds.Should().Be(14 * 3600, "the allowance resets at the next UTC midnight");
    }

    [Fact]
    public async Task Per_learner_cost_ceiling_stops_voice_live_long_before_the_minute_cap()
    {
        // Voice Live is billed at roughly $30 per audio hour, so a $0.60 free ceiling buys about a
        // minute - far less than DailyMinutesPerLearner. That is the intended outcome: the minute cap
        // bounds a session, the cost ceiling bounds the bill, and for realtime audio the bill binds
        // first. Documented here so the 30-minute setting is not mistaken for what a free learner gets.
        var (meter, _, _) = CreateMeter(dailyMinutesPerLearner: 30, freeDailyBudgetPerLearnerUsd: 0.60);
        using var _scope = PushCaller();

        await meter.ReserveAsync("british");

        var act = () => meter.ReserveAsync("british");

        (await act.Should().ThrowAsync<AiAdmissionException>())
            .Which.Code.Should().Be("learner_budget_exhausted");
    }

    [Fact]
    public async Task Settle_clamps_an_inflated_client_duration_to_the_server_clock()
    {
        var (meter, costs, clock) = CreateMeter();
        using var _scope = PushCaller();
        var ticket = await meter.ReserveAsync("british");

        clock.Advance(TimeSpan.FromSeconds(30));
        var settlement = await meter.SettleAsync(ticket.SessionId, reportedSeconds: 600);

        // 30s of server-observed time plus the 5s clock-skew grace — not the 600s claimed.
        settlement.BilledSeconds.Should().Be(35);
        costs.Snapshot().DailyBudgetUsedUsd
            .Should().BeApproximately(35 / 3600d * 30, 0.000001);
    }

    [Fact]
    public async Task Settle_clamps_a_forgotten_tab_to_the_configured_session_ceiling()
    {
        var (meter, costs, clock) = CreateMeter();
        using var _scope = PushCaller();
        var ticket = await meter.ReserveAsync("british");

        // Both clocks agree the tab ran past the ceiling; the ceiling is what protects the budget.
        clock.Advance(TimeSpan.FromMinutes(12));
        var settlement = await meter.SettleAsync(ticket.SessionId, reportedSeconds: 12 * 60);

        settlement.BilledSeconds.Should().Be(600, "MaxSessionMinutes is 10");
        costs.Snapshot().DailyBudgetUsedUsd.Should().BeApproximately(600 / 3600d * 30, 0.000001);
    }

    [Fact]
    public async Task A_reservation_that_outlives_its_grace_window_stays_charged()
    {
        var (meter, costs, clock) = CreateMeter();
        using var _scope = PushCaller();
        var ticket = await meter.ReserveAsync("british");
        var reserved = costs.Snapshot().DailyBudgetUsedUsd;

        // Past MaxSessionMinutes + the settlement grace the record is gone, so a very late report
        // cannot refund anything. The learner keeps the reservation: the intended failure mode.
        clock.Advance(TimeSpan.FromHours(8));
        var settlement = await meter.SettleAsync(ticket.SessionId, reportedSeconds: 30);

        settlement.AlreadySettled.Should().BeTrue();
        costs.Snapshot().DailyBudgetUsedUsd.Should().BeApproximately(reserved, 0.000001);
    }

    [Fact]
    public async Task Settle_returns_the_unused_reservation_to_the_learners_daily_allowance()
    {
        var (meter, _, clock) = CreateMeter();
        using var _scope = PushCaller();
        var ticket = await meter.ReserveAsync("british");
        ticket.RemainingDailyMinutes.Should().Be(28);

        clock.Advance(TimeSpan.FromSeconds(25));
        var settlement = await meter.SettleAsync(ticket.SessionId, reportedSeconds: 25);

        // Billed 25s, so ~29.6 of the 30 minutes are still available rather than 28.
        settlement.RemainingDailyMinutes.Should().BeApproximately(30 - 25 / 60d, 0.0001);
    }

    [Fact]
    public async Task Settling_twice_is_a_no_op_rather_than_a_second_refund()
    {
        var (meter, costs, clock) = CreateMeter();
        using var _scope = PushCaller();
        var ticket = await meter.ReserveAsync("british");
        clock.Advance(TimeSpan.FromSeconds(30));

        await meter.SettleAsync(ticket.SessionId, reportedSeconds: 30);
        var spentAfterFirst = costs.Snapshot().DailyBudgetUsedUsd;

        var replay = await meter.SettleAsync(ticket.SessionId, reportedSeconds: 30);

        replay.AlreadySettled.Should().BeTrue();
        replay.BilledSeconds.Should().Be(0);
        costs.Snapshot().DailyBudgetUsedUsd.Should().BeApproximately(spentAfterFirst, 0.000001);
    }

    [Fact]
    public async Task Settling_an_unknown_session_succeeds_so_a_retrying_client_is_never_blocked()
    {
        var (meter, _, _) = CreateMeter();
        using var _scope = PushCaller();

        var settlement = await meter.SettleAsync(new string('a', 32), reportedSeconds: 30);

        settlement.AlreadySettled.Should().BeTrue();
        settlement.BilledSeconds.Should().Be(0);
    }

    [Fact]
    public async Task A_different_learner_cannot_settle_someone_elses_session()
    {
        var (meter, costs, clock) = CreateMeter();
        string sessionId;
        using (PushCaller())
            sessionId = (await meter.ReserveAsync("british")).SessionId;

        clock.Advance(TimeSpan.FromSeconds(30));
        double spentBefore = costs.Snapshot().DailyBudgetUsedUsd;

        using (PushCaller("intruder"))
        {
            var settlement = await meter.SettleAsync(sessionId, reportedSeconds: 0);
            settlement.AlreadySettled.Should().BeTrue();
        }

        costs.Snapshot().DailyBudgetUsedUsd
            .Should().BeApproximately(spentBefore, 0.000001, "the reservation must not be refunded to a stranger");
    }

    [Fact]
    public async Task Cancel_fully_reverses_a_reservation_whose_session_never_started()
    {
        var (meter, costs, _) = CreateMeter();
        using var _scope = PushCaller();
        var ticket = await meter.ReserveAsync("british");

        await meter.CancelAsync(ticket.SessionId);

        costs.Snapshot().DailyBudgetUsedUsd.Should().Be(0);
        var next = await meter.ReserveAsync("british");
        next.RemainingDailyMinutes.Should().Be(28, "the cancelled minutes went back into the allowance");
    }

    [Fact]
    public async Task Cancelling_an_already_settled_session_does_not_refund_it_again()
    {
        var (meter, costs, clock) = CreateMeter();
        using var _scope = PushCaller();
        var ticket = await meter.ReserveAsync("british");
        clock.Advance(TimeSpan.FromSeconds(30));
        await meter.SettleAsync(ticket.SessionId, reportedSeconds: 30);
        var spent = costs.Snapshot().DailyBudgetUsedUsd;

        await meter.CancelAsync(ticket.SessionId);

        costs.Snapshot().DailyBudgetUsedUsd.Should().BeApproximately(spent, 0.000001);
    }

    [Theory]
    // A client that reports nothing usable is billed the ceiling, never zero.
    [InlineData(double.NaN, 35)]
    [InlineData(double.PositiveInfinity, 35)]
    [InlineData(-100, 0)]
    [InlineData(10, 10)]
    public void Clamp_handles_hostile_and_broken_client_values(double reported, double expected)
    {
        var billed = AzureVoiceLiveSessionMeter.ClampReportedSeconds(
            reported,
            Start,
            Start.AddSeconds(30),
            maxSessionMinutes: 10);

        billed.Should().Be(expected);
    }

    private static IDisposable PushCaller(string caller = Caller) =>
        AiAdmissionContext.Push(new AiAdmissionContext.State(
            caller,
            AiSubscriptionTier.Free,
            AiFeature.SpeakingTutor,
            "/api/speaking/accent-tutors/{tutorId}/voice-live/token"));

    private static (AzureVoiceLiveSessionMeter Meter, VariableCostMeter Costs, MutableTimeProvider Clock) CreateMeter(
        double dailyBudgetUsd = 25,
        int dailyMinutesPerLearner = 30,
        double freeDailyBudgetPerLearnerUsd = 0)
    {
        var clock = new MutableTimeProvider(Start);
        var costs = new VariableCostMeter(
            new AiAdmissionOptions
            {
                DailyBudgetUsd = dailyBudgetUsd,
                PricingConfigured = true,
                // Off by default so each test asserts its own guard. Voice Live is billed at roughly
                // $30/audio-hour, so the real per-learner ceiling is reached in about a minute - see
                // Per_learner_cost_ceiling_stops_voice_live_long_before_the_minute_cap.
                FreeDailyBudgetPerLearnerUsd = freeDailyBudgetPerLearnerUsd,
            },
            clock);
        var options = new AzureVoiceLiveOptions
        {
            PricingConfigured = true,
            CostPerAudioHourUsd = 30,
            MaxSessionMinutes = 10,
            ReservationMinutes = 2,
            DailyMinutesPerLearner = dailyMinutesPerLearner,
        };
        var meter = new AzureVoiceLiveSessionMeter(
            options,
            costs,
            new InMemoryVoiceLiveSessionStore(clock),
            clock,
            NullLogger<AzureVoiceLiveSessionMeter>.Instance);
        return (meter, costs, clock);
    }

    private sealed class MutableTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan duration) => _utcNow = _utcNow.Add(duration);
    }
}
