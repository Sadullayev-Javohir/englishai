using Application.Learning.Ports;
using Application.Learning.StartLearning;
using Domain.Assessment;
using Domain.Learning;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Learning;

public class StartLearningHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    [Fact]
    public async Task Creates_a_profile_seeded_at_the_chosen_level_when_none_exists()
    {
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns((LearnerProfile?)null);
        var handler = new StartLearningCommandHandler(_profiles, _clock);

        await handler.Handle(new StartLearningCommand(Learner, CefrLevel.A1), CancellationToken.None);

        await _profiles.Received(1).SaveAsync(
            Arg.Is<LearnerProfile>(p => p.LearnerId == Learner && p.OverallLevel == CefrLevel.A1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Does_not_overwrite_an_existing_profile()
    {
        var existing = LearnerProfile.CreateAtLevel(Learner, CefrLevel.B2, Now);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(existing);
        var handler = new StartLearningCommandHandler(_profiles, _clock);

        await handler.Handle(new StartLearningCommand(Learner, CefrLevel.A1), CancellationToken.None);

        await _profiles.DidNotReceive().SaveAsync(Arg.Any<LearnerProfile>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Validator_rejects_an_empty_learner_and_an_out_of_range_level()
    {
        var validator = new StartLearningCommandValidator();

        validator.Validate(new StartLearningCommand(Guid.Empty, CefrLevel.A1)).IsValid.Should().BeFalse();
        validator.Validate(new StartLearningCommand(Learner, (CefrLevel)99)).IsValid.Should().BeFalse();
        validator.Validate(new StartLearningCommand(Learner, CefrLevel.A1)).IsValid.Should().BeTrue();
    }
}
