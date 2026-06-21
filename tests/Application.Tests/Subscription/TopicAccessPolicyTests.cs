using Application.Common;
using Application.Learning.Ports;
using Application.Referral.Ports;
using Application.Subscription.Access;
using Application.Subscription.Ports;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Subscription;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Subscription;

/// <summary>
/// Tests the trial paywall service (PROJECT-SPEC H.1): it resolves the learner's privilege and
/// started topics and enforces the free-topic allowance server-side.
/// </summary>
public class TopicAccessPolicyTests
{
    private static readonly DateTimeOffset Now = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IProAccessService _proAccess = Substitute.For<IProAccessService>();
    private readonly IComplimentaryAccess _complimentary = Substitute.For<IComplimentaryAccess>();
    private readonly ITopicCompletionStore _completions = Substitute.For<ITopicCompletionStore>();
    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly ILearnerProfileRepository _profiles = Substitute.For<ILearnerProfileRepository>();
    private readonly IReferralStore _referrals = Substitute.For<IReferralStore>();

    public TopicAccessPolicyTests()
    {
        _proAccess.EvaluateAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new ProAccessDecision(false, false, false, false, null));
    }

    private TopicAccessPolicy CreatePolicy() =>
        new(_proAccess, _complimentary, _completions, _topics, _profiles, _referrals);

    private void StartedTopics(params Guid[] topicIds)
    {
        var records = topicIds
            .Select((id, i) => TopicCompletionRecord.Start(Learner, id, CefrLevel.A2, Now.AddMinutes(i)))
            .ToArray();
        _completions.GetByLearnerAsync(Learner, Arg.Any<CancellationToken>()).Returns(records);
    }

    [Fact]
    public async Task Fourth_topic_is_blocked_for_a_free_learner()
    {
        var topicC = Guid.NewGuid();
        StartedTopics(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        var act = () => CreatePolicy().EnsureAccessAsync(Learner, topicC, CancellationToken.None);

        await act.Should().ThrowAsync<SubscriptionRequiredException>();
    }

    [Fact]
    public async Task First_three_topics_are_free()
    {
        var topicB = Guid.NewGuid();
        StartedTopics(Guid.NewGuid(), Guid.NewGuid()); // two started → third is still free

        var decision = await CreatePolicy().EvaluateAsync(Learner, topicB, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Premium_learner_is_never_blocked()
    {
        _proAccess.EvaluateAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new ProAccessDecision(true, true, false, false, null));
        StartedTopics(Guid.NewGuid(), Guid.NewGuid());

        var decision = await CreatePolicy().EvaluateAsync(Learner, Guid.NewGuid(), CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
        decision.RequiresPro.Should().BeFalse();
    }

    [Fact]
    public async Task Active_Pro_trial_is_never_blocked()
    {
        _proAccess.EvaluateAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new ProAccessDecision(true, false, false, true, Now.AddDays(30)));
        StartedTopics(Guid.NewGuid(), Guid.NewGuid());

        var decision = await CreatePolicy().EvaluateAsync(Learner, Guid.NewGuid(), CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
        decision.RequiresPro.Should().BeFalse();
    }

    [Fact]
    public async Task Comped_learner_is_never_blocked()
    {
        _complimentary.HasFullAccessAsync(Learner, Arg.Any<CancellationToken>()).Returns(true);
        StartedTopics(Guid.NewGuid(), Guid.NewGuid());

        var decision = await CreatePolicy().EvaluateAsync(Learner, Guid.NewGuid(), CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Lower_placement_band_is_fully_open_for_review()
    {
        var topic = Topic(CefrLevel.A2, "review");
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Profile(CefrLevel.B1));

        var decision = await CreatePolicy().EnsureLearningAccessAsync(
            Learner, topic.Id, SkillType.Listening, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Higher_level_topic_is_progression_locked()
    {
        var topic = Topic(CefrLevel.C1, "advanced");
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Profile(CefrLevel.B1));

        var act = () => CreatePolicy().EnsureLearningAccessAsync(
            Learner, topic.Id, SkillType.Vocabulary, CancellationToken.None);

        await act.Should().ThrowAsync<TopicLockedException>();
    }

    [Fact]
    public async Task Current_band_skills_are_not_order_locked()
    {
        var topic = Topic(CefrLevel.B1, "frontier");
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Profile(CefrLevel.B1));

        var decision = await CreatePolicy().EnsureLearningAccessAsync(
            Learner, topic.Id, SkillType.Writing, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    [Fact]
    public async Task Current_band_topics_are_not_order_locked()
    {
        var topic = Topic(CefrLevel.B1, "later");
        _topics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);
        _profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(Profile(CefrLevel.B1));

        var decision = await CreatePolicy().EnsureLearningAccessAsync(
            Learner, topic.Id, SkillType.Listening, CancellationToken.None);

        decision.IsAllowed.Should().BeTrue();
    }

    private static LearnerProfile Profile(CefrLevel level) =>
        LearnerProfile.CreateAtLevel(Learner, level, Now);

    private static VocabularyTopic Topic(CefrLevel level, string key) =>
        VocabularyTopic.Curate($"{level}-{key}", key, key, "daily life", "present-simple", level, Now);
}
