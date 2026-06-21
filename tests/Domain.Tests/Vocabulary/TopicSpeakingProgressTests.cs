using Domain.Common;
using Domain.Vocabulary;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Vocabulary;

public class TopicSpeakingProgressTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 23, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Start_initializes_an_unlearned_record()
    {
        var progress = TopicSpeakingProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);

        progress.SpokenSeconds.Should().Be(0);
        progress.IsLearned.Should().BeFalse();
        progress.LearnedAt.Should().BeNull();
    }

    [Fact]
    public void Start_rejects_empty_ids()
    {
        var act1 = () => TopicSpeakingProgress.Start(Guid.Empty, Guid.NewGuid(), Now);
        var act2 = () => TopicSpeakingProgress.Start(Guid.NewGuid(), Guid.Empty, Now);

        act1.Should().Throw<DomainException>();
        act2.Should().Throw<DomainException>();
    }

    [Fact]
    public void AddSpeaking_accumulates_time_below_the_goal_without_learning()
    {
        var progress = TopicSpeakingProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);

        var justLearned = progress.AddSpeaking(TimeSpan.FromMinutes(3), Now);

        justLearned.Should().BeFalse();
        progress.IsLearned.Should().BeFalse();
        progress.SpokenTime.Should().Be(TimeSpan.FromMinutes(3));
    }

    [Fact]
    public void AddSpeaking_marks_learned_once_the_five_minute_goal_is_reached()
    {
        var progress = TopicSpeakingProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);
        progress.AddSpeaking(TimeSpan.FromMinutes(3), Now);

        var justLearned = progress.AddSpeaking(TimeSpan.FromMinutes(2), Now.AddMinutes(3));

        justLearned.Should().BeTrue();
        progress.IsLearned.Should().BeTrue();
        progress.LearnedAt.Should().Be(Now.AddMinutes(3));
    }

    [Fact]
    public void AddSpeaking_flags_just_learned_only_on_the_crossing_call()
    {
        var progress = TopicSpeakingProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);
        progress.AddSpeaking(TopicSpeakingProgress.RequiredSpeakingTime, Now).Should().BeTrue();

        // Already learned: further speaking accumulates time but does not re-flag.
        progress.AddSpeaking(TimeSpan.FromMinutes(1), Now.AddMinutes(6)).Should().BeFalse();
        progress.IsLearned.Should().BeTrue();
    }

    [Fact]
    public void AddSpeaking_ignores_a_non_positive_delta()
    {
        var progress = TopicSpeakingProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);

        progress.AddSpeaking(TimeSpan.Zero, Now).Should().BeFalse();
        progress.AddSpeaking(TimeSpan.FromSeconds(-30), Now).Should().BeFalse();
        progress.SpokenSeconds.Should().Be(0);
    }

    [Fact]
    public void Practice_completion_requires_three_sessions_across_two_days()
    {
        var progress = TopicSpeakingProgress.Start(Guid.NewGuid(), Guid.NewGuid(), Now);
        progress.AddSpeaking(TimeSpan.FromMinutes(5), Now);

        progress.RecordSession(Guid.NewGuid(), Now, 80);
        progress.RecordSession(Guid.NewGuid(), Now.AddHours(2), 75);
        progress.HasSufficientPracticeEvidence.Should().BeFalse();

        progress.RecordSession(Guid.NewGuid(), Now.AddDays(1), 70);

        progress.HasSufficientPracticeEvidence.Should().BeTrue();
        progress.SessionCount.Should().Be(3);
        progress.PracticeDayCount.Should().Be(2);
        progress.ConservativeMasteryScore.Should().Be(70);
    }
}
