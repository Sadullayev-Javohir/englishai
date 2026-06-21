using Application.Analytics.Ports;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Learning.Dtos;
using Application.Learning.GetProgressInsight;
using Application.Learning.Ports;
using Application.Vocabulary.Ports;
using Domain.Analytics;
using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Learning;

public class GetProgressInsightQueryHandlerTests
{
    private static readonly Guid Learner = Guid.NewGuid();
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = DateOnly.FromDateTime(Now.UtcDateTime);

    [Fact]
    public async Task Handler_builds_bounded_snapshot_and_joins_server_evidence()
    {
        var profile = LearnerProfile.CreateFromPlacement(Learner,
            new PlacementResult(CefrLevel.B1, 50, new Dictionary<TestStage, StageResult>()), Now.AddDays(-60));
        profile.RecordActivity(SkillType.Speaking, 35, Now.AddDays(-10));
        profile.RecordActivity(SkillType.Speaking, 55, Now);
        profile.RecordError(ErrorCategory.Articles, SkillType.Speaking, Now);
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(profile);
        var study = Substitute.For<IStudyLogStore>();
        study.GetStatsAsync(Learner, Today, Arg.Any<CancellationToken>()).Returns(StudyStats.Empty(Today));
        var vocabulary = Substitute.For<IVocabularyStatsReader>();
        vocabulary.ReadAsync(Learner, Now, Arg.Any<CancellationToken>()).Returns(
            new VocabularyStats(4, 3, 1, 2, 1, 4, 2,
                Enum.GetValues<ReviewStage>().ToDictionary(stage => stage, _ => 0)));
        var gamification = Substitute.For<IGamificationStore>();
        gamification.GetCompletedDaysAsync(Learner, Arg.Any<CancellationToken>()).Returns([]);
        gamification.GetSkillsPracticedTodayAsync(Learner, Today, Arg.Any<CancellationToken>()).Returns([]);
        var preferences = Substitute.For<IUserPreferencesStore>();
        var generator = Substitute.For<IProgressInsightGenerator>();
        generator.GenerateAsync(Arg.Any<ProgressSnapshotDto>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var snapshot = call.Arg<ProgressSnapshotDto>();
                snapshot.Skills.Should().HaveCount(6);
                snapshot.Skills.Single(skill => skill.Skill == SkillType.Speaking).Confidence
                    .Should().Be(ProgressDataConfidence.Low);
                snapshot.ErrorsLast30Days.Single().CountLast30Days.Should().Be(1);
                snapshot.ErrorsLast30Days.Single().RecentExamples.Should().ContainSingle();
                snapshot.Vocabulary.Due.Should().Be(2);
                return new GeneratedProgressInsight("needs_focus", [],
                    [new GeneratedSkillInsight(SkillType.Speaking, 1, "lowest_score", "practice_speaking")],
                    [new GeneratedErrorInsight(ErrorCategory.Articles, 1, "drill_articles")],
                    "start_habit", "practice_speaking", "/speaking", ProgressInsightSource.Hermes, false);
            });
        var topics = Substitute.For<ITopicCompletionStore>();
        topics.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetProgressInsightQueryHandler(profiles, study, vocabulary, topics, gamification,
            preferences, generator, new FixedTimeProvider(Now));

        var result = await handler.Handle(new GetProgressInsightQuery(Learner, Today), CancellationToken.None);

        result.Insight.Source.Should().Be(ProgressInsightSource.Hermes);
        result.Insight.SkillsToStrengthen.Single().Score.Should().BeGreaterThan(35);
        result.Insight.RecurringErrors.Single().CountLast30Days.Should().Be(1);
    }

    [Fact]
    public async Task Missing_profile_returns_valid_empty_snapshot_instead_of_404()
    {
        var profiles = Substitute.For<ILearnerProfileRepository>();
        var study = Substitute.For<IStudyLogStore>();
        study.GetStatsAsync(Learner, Today, Arg.Any<CancellationToken>()).Returns(StudyStats.Empty(Today));
        var vocabulary = Substitute.For<IVocabularyStatsReader>();
        vocabulary.ReadAsync(Learner, Now, Arg.Any<CancellationToken>()).Returns(
            new VocabularyStats(0, 0, 0, 0, 0, 0, 0,
                Enum.GetValues<ReviewStage>().ToDictionary(stage => stage, _ => 0)));
        var gamification = Substitute.For<IGamificationStore>();
        gamification.GetCompletedDaysAsync(Learner, Arg.Any<CancellationToken>()).Returns([]);
        gamification.GetSkillsPracticedTodayAsync(Learner, Today, Arg.Any<CancellationToken>()).Returns([]);
        var generator = Substitute.For<IProgressInsightGenerator>();
        generator.GenerateAsync(Arg.Any<ProgressSnapshotDto>(), Arg.Any<CancellationToken>()).Returns(
            new GeneratedProgressInsight("no_data", [], [], [], "no_study_data", "start_placement",
                "/assessment", ProgressInsightSource.Local, true));
        var topics = Substitute.For<ITopicCompletionStore>();
        topics.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>()).Returns([]);
        var handler = new GetProgressInsightQueryHandler(profiles, study, vocabulary, topics, gamification,
            Substitute.For<IUserPreferencesStore>(), generator, new FixedTimeProvider(Now));

        var result = await handler.Handle(new GetProgressInsightQuery(Learner, Today), CancellationToken.None);

        result.Snapshot.HasLearningProfile.Should().BeFalse();
        result.Snapshot.Skills.Should().BeEmpty();
        result.Insight.TargetRoute.Should().Be("/assessment");
    }

    [Fact]
    public async Task Cancellation_is_forwarded_to_first_store_read()
    {
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, cancellation.Token)
            .Returns<Task<LearnerProfile?>>(_ => throw new OperationCanceledException(cancellation.Token));
        var handler = new GetProgressInsightQueryHandler(profiles, Substitute.For<IStudyLogStore>(),
            Substitute.For<IVocabularyStatsReader>(), Substitute.For<ITopicCompletionStore>(),
            Substitute.For<IGamificationStore>(),
            Substitute.For<IUserPreferencesStore>(), Substitute.For<IProgressInsightGenerator>(),
            new FixedTimeProvider(Now));

        var act = () => handler.Handle(new GetProgressInsightQuery(Learner, Today), cancellation.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }
}
