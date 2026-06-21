using Application.Assessment.FinalizePlacementTest;
using Application.Assessment.Ports;
using Application.Common;
using Application.Competition.Advance;
using Application.Competition.Finish;
using Application.Competition.Ports;
using Application.Competition.SubmitAnswer;
using Application.Gamification.Ports;
using Application.Learning.Ports;
using Application.Speaking.EvaluateRoleplay;
using Application.Speaking.Ports;
using Application.Speaking.SubmitUtterance;
using Application.Vocabulary.Ports;
using Application.Vocabulary.SubmitReview;
using Domain.Assessment;
using Domain.Competition;
using Domain.Speaking;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Security;

using CompetitionAggregate = Domain.Competition.Competition;

public sealed class ObjectAuthorizationTests
{
    private static readonly Guid Owner = Guid.NewGuid();
    private static readonly Guid Caller = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 21, 0, 0, 0, TimeSpan.Zero);

    private static ICurrentUserAccessor Current(Guid learnerId)
    {
        var current = Substitute.For<ICurrentUserAccessor>();
        current.LearnerId.Returns(learnerId);
        return current;
    }

    [Fact]
    public async Task SubmitReview_rejects_a_different_learner_item()
    {
        var repo = Substitute.For<IVocabularyRepository>();
        var item = VocabularyItem.Learn(Owner, "secure", "xavfsiz", Now);
        repo.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new SubmitReviewCommandHandler(
            repo, Substitute.For<IWordUsageAssessor>(), TimeProvider.System, Current(Caller));

        var act = () => handler.Handle(
            new SubmitReviewCommand(item.Id, SelfRatedPassed: true), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await repo.DidNotReceive().SaveAsync(Arg.Any<VocabularyItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitUtterance_rejects_a_different_learner_session_before_paid_services()
    {
        var conversations = Substitute.For<IConversationStore>();
        var session = ConversationSession.Start(Owner, CefrLevel.B1);
        conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var stt = Substitute.For<ISpeechToTextService>();
        var handler = new SubmitUtteranceCommandHandler(
            conversations, stt, Substitute.For<IPronunciationAssessor>(), Substitute.For<IConversationTutor>(),
            Substitute.For<ITextToSpeechService>(), Substitute.For<IFeedbackTemplateProvider>(),
            Substitute.For<ITopicSpeakingProgressStore>(), Substitute.For<IVocabularyTopicRepository>(),
            Substitute.For<ITopicCompletionStore>(), Substitute.For<Application.Gamification.IDailyProgressRecorder>(),
            Substitute.For<Application.Identity.Ports.IUserAccountStore>(),
            Substitute.For<Application.Analytics.Ports.IProductEventStore>(), TimeProvider.System,
            currentUser: Current(Caller));

        var act = () => handler.Handle(new SubmitUtteranceCommand(session.Id, new byte[] { 1 }), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await stt.DidNotReceiveWithAnyArgs().TranscribeAsync(default!, default);
    }

    [Fact]
    public async Task EvaluateRoleplay_rejects_a_different_learner_session()
    {
        var conversations = Substitute.For<IConversationStore>();
        var session = ConversationSession.Start(Owner, CefrLevel.B1, scenarioCode: "restaurant");
        conversations.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var evaluator = Substitute.For<IRoleplayEvaluator>();
        var handler = new EvaluateRoleplayCommandHandler(
            conversations, evaluator, Substitute.For<IFeedbackTemplateProvider>(), Current(Caller));

        var act = () => handler.Handle(new EvaluateRoleplayCommand(session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await evaluator.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default!, default);
    }

    [Fact]
    public async Task FinalizePlacement_rejects_a_different_learner_session()
    {
        var sessions = Substitute.For<IPlacementSessionStore>();
        var session = PlacementTestSession.Start(Owner, includeSpeaking: false);
        sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var handler = new FinalizePlacementTestCommandHandler(
            sessions, Substitute.For<ILearnerProfileRepository>(),
            Substitute.For<Application.Analytics.Ports.IProductEventStore>(), TimeProvider.System, Current(Caller));

        var act = () => handler.Handle(new FinalizePlacementTestCommand(session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await sessions.DidNotReceive().SaveAsync(Arg.Any<PlacementTestSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FinalizeLevelExit_rejects_a_different_learner_session()
    {
        var sessions = Substitute.For<IPlacementSessionStore>();
        var session = PlacementTestSession.Start(Owner, includeSpeaking: false);
        sessions.GetAsync(session.Id, Arg.Any<CancellationToken>()).Returns(session);
        var handler = new Application.Levels.FinalizeLevelExitTest.FinalizeLevelExitTestCommandHandler(
            sessions, Substitute.For<ILearnerProfileRepository>(), Substitute.For<ILeaderboardStore>(),
            Substitute.For<ILearnerPointsRepository>(), TimeProvider.System, Current(Caller));

        var act = () => handler.Handle(
            new Application.Levels.FinalizeLevelExitTest.FinalizeLevelExitTestCommand(session.Id), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await sessions.DidNotReceive().SaveAsync(Arg.Any<PlacementTestSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Competition_answer_rejects_a_caller_who_does_not_own_the_participant()
    {
        var competition = CompetitionAggregate.Create(Owner, "Owner", "Test", new CompetitionSettings(), new[] { Guid.NewGuid() });
        var participant = competition.Participants.Single();
        var repo = Substitute.For<ICompetitionRepository>();
        repo.GetByIdAsync(competition.Id, Arg.Any<CancellationToken>()).Returns(competition);
        var handler = new SubmitSlideAnswerCommandHandler(repo, Current(Caller));

        var act = () => handler.Handle(
            new SubmitSlideAnswerCommand(competition.Id, participant.Id, 0, 1), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await repo.DidNotReceive().UpdateAsync(Arg.Any<CompetitionAggregate>(), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task Competition_host_mutations_reject_a_non_host(bool advance)
    {
        var competition = CompetitionAggregate.Create(Owner, "Owner", "Test", new CompetitionSettings(), new[] { Guid.NewGuid() });
        var repo = Substitute.For<ICompetitionRepository>();
        repo.GetByIdAsync(competition.Id, Arg.Any<CancellationToken>()).Returns(competition);

        Func<Task> act = advance
            ? () => new AdvanceSlideCommandHandler(repo, Current(Caller)).Handle(
                new AdvanceSlideCommand(competition.Id, Owner), CancellationToken.None)
            : async () => await new FinishCompetitionCommandHandler(repo, Current(Caller)).Handle(
                new FinishCompetitionCommand(competition.Id, Owner), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
        await repo.DidNotReceive().UpdateAsync(Arg.Any<CompetitionAggregate>(), Arg.Any<CancellationToken>());
    }
}
