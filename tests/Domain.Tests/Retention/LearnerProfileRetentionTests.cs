using Domain.Assessment;
using Domain.Learning;
using Domain.Retention;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Retention;

/// <summary>
/// Unit tests for the retention helpers added to <see cref="LearnerProfile"/> in Faza 7
/// (PROJECT-SPEC I.1/I.2/I.4): engagement tracking, speaking-frustration detection, active
/// days, and win-back stage dedup. All times are passed explicitly (docs/development-guide.md 17.2).
/// </summary>
public class LearnerProfileRetentionTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private static LearnerProfile NewProfile(DateTimeOffset createdAt) =>
        LearnerProfile.CreateFromPlacement(
            Learner,
            new PlacementResult(CefrLevel.B1, CefrLevel.B1.ToScore(), new Dictionary<TestStage, StageResult>()),
            createdAt);

    [Fact]
    public void LastActivityAt_starts_at_creation_and_advances_with_activity()
    {
        var profile = NewProfile(Now.AddDays(-10));
        profile.LastActivityAt.Should().Be(Now.AddDays(-10));

        profile.RecordActivity(SkillType.Reading, 80, Now.AddDays(-2));
        profile.LastActivityAt.Should().Be(Now.AddDays(-2));

        // An older backfilled activity must not move the last-activity timestamp backwards.
        profile.RecordActivity(SkillType.Reading, 70, Now.AddDays(-5));
        profile.LastActivityAt.Should().Be(Now.AddDays(-2));
    }

    [Fact]
    public void DaysSinceLastActivity_is_floored_and_never_negative()
    {
        var profile = NewProfile(Now.AddDays(-3));
        profile.DaysSinceLastActivity(Now).Should().Be(3);

        // A clock slightly behind the last activity yields 0, not a negative number.
        var future = NewProfile(Now);
        future.DaysSinceLastActivity(Now.AddDays(-1)).Should().Be(0);
    }

    [Fact]
    public void Recent_low_speaking_score_with_nothing_since_is_frustration()
    {
        var profile = NewProfile(Now.AddDays(-5));
        profile.RecordActivity(SkillType.Speaking, LearnerProfile.SpeakingFrustrationThreshold - 1, Now.AddDays(-1));

        profile.HasUnresolvedSpeakingFrustration(Now).Should().BeTrue();
    }

    [Fact]
    public void Practising_after_a_low_speaking_score_resolves_the_frustration()
    {
        var profile = NewProfile(Now.AddDays(-5));
        profile.RecordActivity(SkillType.Speaking, 40, Now.AddDays(-2));
        profile.RecordActivity(SkillType.Reading, 75, Now.AddDays(-1)); // practised since

        profile.HasUnresolvedSpeakingFrustration(Now).Should().BeFalse();
    }

    [Fact]
    public void A_good_speaking_score_is_not_frustration()
    {
        var profile = NewProfile(Now.AddDays(-5));
        profile.RecordActivity(SkillType.Speaking, 85, Now.AddDays(-1));

        profile.HasUnresolvedSpeakingFrustration(Now).Should().BeFalse();
    }

    [Fact]
    public void ActiveDays_returns_distinct_utc_days()
    {
        var profile = NewProfile(Now.AddDays(-5));
        profile.RecordActivity(SkillType.Reading, 80, Now.AddDays(-2));
        profile.RecordActivity(SkillType.Speaking, 80, Now.AddDays(-2).AddHours(3)); // same day
        profile.RecordActivity(SkillType.Reading, 80, Now.AddDays(-1));

        profile.ActiveDays().Should().HaveCount(2);
    }

    [Fact]
    public void Win_back_stage_records_and_resets()
    {
        var profile = NewProfile(Now.AddDays(-30));
        profile.LastWinBackStage.Should().Be(InactivityStage.Active);

        profile.RecordWinBack(InactivityStage.Day7, Now);
        profile.LastWinBackStage.Should().Be(InactivityStage.Day7);

        profile.ResetWinBack();
        profile.LastWinBackStage.Should().Be(InactivityStage.Active);
    }
}
