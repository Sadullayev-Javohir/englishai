using Application.Common;
using Application.Identity.Ports;
using Application.Learning.GetRecommendedTopics;
using Application.Learning.Ports;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Common;
using Domain.Identity;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Learning;

public class GetRecommendedTopicsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();

    private GetRecommendedTopicsQueryHandler CreateHandler() => new(_accounts, _profiles, _topics);

    // Topic with an explicit category + sequence so ordering is deterministic.
    private static VocabularyTopic Topic(string en, string category, int sequence) =>
        VocabularyTopic.Curate($"b1-{en}".ToLowerInvariant(), en, en, category, "present-perfect",
            CefrLevel.B1, Now, sequence);

    private UserAccount GivenAccount(LearningGoal goal)
    {
        var account = UserAccount.Register("sub", "me@example.com", "Aziz", null, Now);
        _accounts.GetByIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(account);

        // The goal now lives on the learner profile; seed a B1 profile carrying the chosen goal.
        var profile = LearnerProfile.CreateAtLevel(account.Id, CefrLevel.B1, Now);
        profile.SetLearningGoal(goal);
        _profiles.GetByLearnerIdAsync(account.Id, Arg.Any<CancellationToken>()).Returns(profile);
        return account;
    }

    [Fact]
    public async Task Bubbles_goal_relevant_topics_to_the_top()
    {
        var account = GivenAccount(LearningGoal.Work);
        // Neutral topic first by sequence, but a work_jobs topic should still win on relevance.
        var neutral = Topic("Family Life", "family_people", sequence: 0);
        var work = Topic("Job Interview", "work_jobs", sequence: 5);
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { neutral, work });

        var result = await CreateHandler().Handle(
            new GetRecommendedTopicsQuery(account.Id, 6), CancellationToken.None);

        result.Goal.Should().Be(LearningGoal.Work);
        result.Level.Should().Be(CefrLevel.B1);
        result.Topics[0].Id.Should().Be(work.Id);
        result.Topics[0].IsGoalRelevant.Should().BeTrue();
        result.Topics[1].Id.Should().Be(neutral.Id);
        result.Topics[1].IsGoalRelevant.Should().BeFalse();
    }

    [Fact]
    public async Task Unspecified_goal_preserves_plain_catalog_sequence_order()
    {
        var account = GivenAccount(LearningGoal.Unspecified);
        var t0 = Topic("Alpha", "work_jobs", sequence: 0);
        var t1 = Topic("Beta", "family_people", sequence: 1);
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>())
            .Returns(new[] { t1, t0 }); // deliberately out of order

        var result = await CreateHandler().Handle(
            new GetRecommendedTopicsQuery(account.Id, 6), CancellationToken.None);

        result.Topics.Select(t => t.Id).Should().Equal(t0.Id, t1.Id); // sorted by Sequence
        result.Topics.Should().OnlyContain(t => !t.IsGoalRelevant);
    }

    [Fact]
    public async Task Respects_the_count_limit()
    {
        var account = GivenAccount(LearningGoal.Travel);
        var topics = Enumerable.Range(0, 10)
            .Select(i => Topic($"T{i}", "food_drink", i))
            .ToArray();
        _topics.GetByLevelAsync(CefrLevel.B1, Arg.Any<CancellationToken>()).Returns(topics);

        var result = await CreateHandler().Handle(
            new GetRecommendedTopicsQuery(account.Id, 3), CancellationToken.None);

        result.Topics.Should().HaveCount(3);
    }

    [Fact]
    public async Task Missing_account_throws_not_found()
    {
        _accounts.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((UserAccount?)null);

        var act = () => CreateHandler().Handle(
            new GetRecommendedTopicsQuery(Guid.NewGuid(), 6), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
