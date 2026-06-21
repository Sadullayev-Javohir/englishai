using Application.Ai;
using FluentAssertions;
using Infrastructure.Ai;
using Xunit;

namespace Integration.Tests.Ai;

public sealed class AiAdmissionControlTests
{
    [Fact]
    public void Default_feature_limits_are_balanced_for_interactive_ai_use()
    {
        var options = new AiAdmissionOptions();

        options.FreeRequestsPerMinute.Should().Be(15);
        options.ProRequestsPerMinute.Should().Be(30);
        options.AssistantFreeRequestsPerMinute.Should().Be(20);
        options.AssistantProRequestsPerMinute.Should().Be(40);
    }

    [Fact]
    public async Task Free_limit_returns_canonical_rate_limited_error()
    {
        var admission = Create(new AiAdmissionOptions { GlobalConcurrency = 2, ProReservedConcurrency = 1, AssistantFreeRequestsPerMinute = 1, AssistantProRequestsPerMinute = 4 });
        await using var first = await admission.AcquireAsync(Request("free", AiSubscriptionTier.Free), default);
        first.Complete(10);

        var error = await FluentActions.Awaiting(() => admission.AcquireAsync(Request("free", AiSubscriptionTier.Free), default)).Should().ThrowAsync<AiAdmissionException>();
        error.Which.Code.Should().Be("rate_limited");
        error.Which.StatusCode.Should().Be(429);
        error.Which.RetryAfterSeconds.Should().BePositive();
    }

    [Fact]
    public async Task Assistant_uses_feature_specific_limits()
    {
        var admission = Create(new AiAdmissionOptions
        {
            GlobalConcurrency = 2,
            ProReservedConcurrency = 1,
            FreeRequestsPerMinute = 1,
            ProRequestsPerMinute = 1,
            AssistantFreeRequestsPerMinute = 2,
            AssistantProRequestsPerMinute = 3,
        });

        await using (var first = await admission.AcquireAsync(Request("free", AiSubscriptionTier.Free), default)) first.Complete(1);
        await using (var second = await admission.AcquireAsync(Request("free", AiSubscriptionTier.Free), default)) second.Complete(1);

        await FluentActions.Awaiting(() => admission.AcquireAsync(Request("free", AiSubscriptionTier.Free), default))
            .Should().ThrowAsync<AiAdmissionException>();
    }

    [Fact]
    public async Task Pro_reserve_remains_available_when_shared_pool_is_busy()
    {
        var admission = Create(new AiAdmissionOptions { GlobalConcurrency = 2, ProReservedConcurrency = 1, MaxQueueLength = 2, QueueTimeoutSeconds = 1, FreeRequestsPerMinute = 10, ProRequestsPerMinute = 10 });
        await using var free = await admission.AcquireAsync(Request("free", AiSubscriptionTier.Free), default);
        await using var pro = await admission.AcquireAsync(Request("pro", AiSubscriptionTier.Pro), default);
        admission.Snapshot().Active.Should().Be(2);
    }

    [Fact]
    public async Task One_caller_cannot_consume_every_global_slot()
    {
        var admission = Create(new AiAdmissionOptions
        {
            GlobalConcurrency = 4,
            ProReservedConcurrency = 0,
            MaxConcurrentPerCaller = 1,
            MaxQueueLength = 8,
            QueueTimeoutSeconds = 1,
            FreeRequestsPerMinute = 10,
            ProRequestsPerMinute = 10,
        });

        await using var firstUserLease = await admission.AcquireAsync(Request("user-one", AiSubscriptionTier.Free), default);
        var blockedSameUser = admission.AcquireAsync(Request("user-one", AiSubscriptionTier.Free), default);

        await using var secondUserLease = await admission.AcquireAsync(Request("user-two", AiSubscriptionTier.Free), default);
        admission.Snapshot().Active.Should().Be(2);

        var error = await FluentActions.Awaiting(() => blockedSameUser).Should().ThrowAsync<AiAdmissionException>();
        error.Which.Code.Should().Be("timeout");
    }

    [Fact]
    public async Task Queue_wait_is_bounded_and_reports_timeout()
    {
        var admission = Create(new AiAdmissionOptions { GlobalConcurrency = 1, ProReservedConcurrency = 0, MaxQueueLength = 2, QueueTimeoutSeconds = 1, FreeRequestsPerMinute = 10, ProRequestsPerMinute = 10 });
        await using var held = await admission.AcquireAsync(Request("one", AiSubscriptionTier.Free), default);
        var error = await FluentActions.Awaiting(() => admission.AcquireAsync(Request("two", AiSubscriptionTier.Free), default)).Should().ThrowAsync<AiAdmissionException>();
        error.Which.Code.Should().Be("timeout");
        admission.Snapshot().Queued.Should().Be(0);
    }

    [Fact]
    public async Task Daily_budget_sheds_free_but_preserves_pro()
    {
        var admission = Create(new AiAdmissionOptions
        {
            GlobalConcurrency = 2, ProReservedConcurrency = 1, FreeRequestsPerMinute = 10, ProRequestsPerMinute = 10,
            DailyBudgetUsd = 0.001, BudgetWarningRatio = 0.8, EstimatedInputCostPerMillionTokensUsd = 1000,
        });
        await using (var lease = await admission.AcquireAsync(new("seed", AiSubscriptionTier.Pro, AiFeature.Assistant, 2, 10), default)) lease.Complete(0);
        admission.Snapshot().FreeTierShed.Should().BeTrue();
        await FluentActions.Awaiting(() => admission.AcquireAsync(Request("free", AiSubscriptionTier.Free), default)).Should().ThrowAsync<AiAdmissionException>();
        await using var pro = await admission.AcquireAsync(Request("pro", AiSubscriptionTier.Pro), default);
    }

    [Fact]
    public async Task Translation_bypasses_per_minute_and_free_budget_shedding()
    {
        var admission = Create(new AiAdmissionOptions
        {
            GlobalConcurrency = 2,
            ProReservedConcurrency = 1,
            FreeRequestsPerMinute = 1,
            ProRequestsPerMinute = 1,
            DailyBudgetUsd = 0.001,
            EstimatedInputCostPerMillionTokensUsd = 1000,
        });
        await using (var seed = await admission.AcquireAsync(
                         new("seed", AiSubscriptionTier.Pro, AiFeature.Assistant, 2, 10), default))
            seed.Complete(0);

        for (var attempt = 0; attempt < 3; attempt++)
        {
            await using var translation = await admission.AcquireAsync(
                new("free", AiSubscriptionTier.Free, AiFeature.Translation, 10, 100), default);
            translation.Complete(10);
        }
    }

    private static AiAdmissionControl Create(AiAdmissionOptions options) => new(options, TimeProvider.System);
    private static AiAdmissionRequest Request(string caller, AiSubscriptionTier tier) => new(caller, tier, AiFeature.Assistant, 10, 100);
}
