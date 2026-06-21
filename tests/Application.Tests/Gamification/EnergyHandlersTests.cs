using Application.Gamification.ConsumeEnergy;
using Application.Gamification.Dtos;
using Application.Gamification.GetEnergy;
using Application.Gamification.Ports;
using Application.Tests.Learning;
using Domain.Gamification;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Gamification;

public class EnergyHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 9, 20, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();
    private readonly ILearnerPointsRepository _points = Substitute.For<ILearnerPointsRepository>();

    [Fact]
    public async Task Getting_energy_returns_a_full_balance_without_timers()
    {
        var ledger = LearnerPoints.CreateNew(Learner, Now);
        _points.GetOrCreateAsync(Learner, Now, Arg.Any<CancellationToken>()).Returns(ledger);

        var result = await new GetEnergyQueryHandler(_points, new FixedTimeProvider(Now))
            .Handle(new GetEnergyQuery(Learner), CancellationToken.None);

        result.Current.Should().Be(5);
        result.NextRefillAt.Should().BeNull();
        result.FullRefillAt.Should().BeNull();
        result.Outcome.Should().Be(EnergyOutcome.None);
    }

    [Theory]
    [InlineData(EnergyAction.Video, "dQw4w9WgXcQ")]
    [InlineData(EnergyAction.Speaking, "5d3a0d3e-a4f5-4e5d-a27c-e81d60335b8c")]
    public async Task Consume_returns_the_atomic_happy_path_balance(EnergyAction action, string referenceId)
    {
        var next = Now.AddMinutes(36);
        var full = Now.AddMinutes(144);
        _points.ConsumeEnergyAsync(Learner, action, referenceId, Now, Arg.Any<CancellationToken>())
            .Returns(new EnergyBalance(1, next, full, EnergyOutcome.Consumed));

        var result = await Handler().Handle(new ConsumeEnergyCommand(Learner, action, referenceId), CancellationToken.None);

        result.Current.Should().Be(1);
        result.NextRefillAt.Should().Be(next);
        result.FullRefillAt.Should().Be(full);
        result.Outcome.Should().Be(EnergyOutcome.Consumed);
    }

    [Fact]
    public async Task Reopening_a_paid_reference_is_already_started_and_is_not_debited()
    {
        _points.ConsumeEnergyAsync(Learner, EnergyAction.Video, "dQw4w9WgXcQ", Now, Arg.Any<CancellationToken>())
            .Returns(new EnergyBalance(3, Now.AddMinutes(36), Now.AddMinutes(72), EnergyOutcome.AlreadyStarted));

        var result = await Handler().Handle(
            new ConsumeEnergyCommand(Learner, EnergyAction.Video, "dQw4w9WgXcQ"), CancellationToken.None);

        result.Current.Should().Be(3);
        result.Outcome.Should().Be(EnergyOutcome.AlreadyStarted);
    }

    [Fact]
    public async Task Empty_bar_returns_insufficient_with_the_real_refill_times()
    {
        var next = Now.AddMinutes(36);
        var full = Now.AddHours(3);
        _points.ConsumeEnergyAsync(Learner, EnergyAction.Video, "dQw4w9WgXcQ", Now, Arg.Any<CancellationToken>())
            .Returns(new EnergyBalance(0, next, full, EnergyOutcome.Insufficient));

        var result = await Handler().Handle(
            new ConsumeEnergyCommand(Learner, EnergyAction.Video, "dQw4w9WgXcQ"), CancellationToken.None);

        result.Outcome.Should().Be(EnergyOutcome.Insufficient);
        result.NextRefillAt.Should().Be(next);
        result.FullRefillAt.Should().Be(full);
    }

    private ConsumeEnergyCommandHandler Handler() => new(_points, new FixedTimeProvider(Now));
}
