using Application.Assessment;
using Application.Assessment.Ports;
using Application.Assessment.ReportIntegrityViolation;
using Application.Common;
using Domain.Assessment;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Assessment;

public class ReportPlacementIntegrityViolationCommandHandlerTests
{
    private readonly IPlacementSessionStore _sessions = Substitute.For<IPlacementSessionStore>();

    [Fact]
    public async Task Handle_records_unique_incidents_idempotently_and_invalidates_at_the_threshold()
    {
        var session = PlacementTestSession.Start(Guid.NewGuid());
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var handler = new ReportPlacementIntegrityViolationCommandHandler(_sessions);
        var firstId = Guid.NewGuid();

        var first = await handler.Handle(
            new ReportPlacementIntegrityViolationCommand(session.Id, firstId, "window_blur"),
            CancellationToken.None);
        var duplicate = await handler.Handle(
            new ReportPlacementIntegrityViolationCommand(session.Id, firstId, "window_blur"),
            CancellationToken.None);

        first.Should().Be(Expected(1, invalidated: false));
        duplicate.Should().Be(Expected(1, invalidated: false));

        ReportPlacementIntegrityViolationResult? last = null;
        for (var count = 2; count <= PlacementTestSession.ViolationsBeforeInvalidation; count++)
        {
            last = await handler.Handle(
                new ReportPlacementIntegrityViolationCommand(session.Id, Guid.NewGuid(), "fullscreen_exit"),
                CancellationToken.None);
            last.Invalidated.Should().Be(count >= PlacementTestSession.ViolationsBeforeInvalidation);
        }

        last.Should().Be(Expected(PlacementTestSession.ViolationsBeforeInvalidation, invalidated: true));
        await _sessions.Received(PlacementTestSession.ViolationsBeforeInvalidation + 1)
            .SaveAsync(session, Arg.Any<CancellationToken>());
    }

    /// <summary>A fresh session always reports the first item of the Vocabulary stage.</summary>
    private static ReportPlacementIntegrityViolationResult Expected(int count, bool invalidated) =>
        new(count, invalidated, nameof(TestStage.Vocabulary), 1,
            PlacementTestSession.QuestionsPerStage.Values.Sum());

    [Fact]
    public async Task Handle_throws_when_session_has_expired()
    {
        _sessions.GetAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((PlacementTestSession?)null);
        var handler = new ReportPlacementIntegrityViolationCommandHandler(_sessions);

        var act = () => handler.Handle(
            new ReportPlacementIntegrityViolationCommand(Guid.NewGuid(), Guid.NewGuid(), "visibility_hidden"),
            CancellationToken.None);

        await act.Should().ThrowAsync<PlacementSessionExpiredException>();
    }

    [Fact]
    public async Task Handle_rejects_a_session_owned_by_another_learner()
    {
        var ownerId = Guid.NewGuid();
        var session = PlacementTestSession.Start(ownerId);
        _sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var currentUser = Substitute.For<ICurrentUserAccessor>();
        currentUser.LearnerId.Returns(Guid.NewGuid());
        var handler = new ReportPlacementIntegrityViolationCommandHandler(_sessions, currentUser);

        var act = () => handler.Handle(
            new ReportPlacementIntegrityViolationCommand(session.Id, Guid.NewGuid(), "window_blur"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await _sessions.DidNotReceive().SaveAsync(Arg.Any<PlacementTestSession>(), Arg.Any<CancellationToken>());
    }
}
