using Domain.Assessment;
using Domain.Common;
using Domain.Learning;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Learning;

/// <summary>
/// Covers the onboarding goal (goal-based onboarding) now stored on the learner profile - it is set
/// once the profile exists (after the level-choice flow) and can be changed later from the profile.
/// </summary>
public class LearnerProfileLearningGoalTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 25, 9, 0, 0, TimeSpan.Zero);

    private static LearnerProfile NewProfile() =>
        LearnerProfile.CreateAtLevel(Guid.NewGuid(), CefrLevel.A2, Now);

    [Fact]
    public void LearningGoal_defaults_to_unspecified()
    {
        NewProfile().LearningGoal.Should().Be(LearningGoal.Unspecified);
    }

    [Theory]
    [InlineData(LearningGoal.IeltsCefr)]
    [InlineData(LearningGoal.Work)]
    [InlineData(LearningGoal.Migration)]
    [InlineData(LearningGoal.Travel)]
    [InlineData(LearningGoal.GeneralSpeaking)]
    [InlineData(LearningGoal.School)]
    [InlineData(LearningGoal.Unspecified)]
    public void SetLearningGoal_stores_a_defined_goal(LearningGoal goal)
    {
        var profile = NewProfile();

        profile.SetLearningGoal(goal);

        profile.LearningGoal.Should().Be(goal);
    }

    [Fact]
    public void SetLearningGoal_can_be_changed_later()
    {
        var profile = NewProfile();
        profile.SetLearningGoal(LearningGoal.Travel);

        profile.SetLearningGoal(LearningGoal.Work);

        profile.LearningGoal.Should().Be(LearningGoal.Work);
    }

    [Fact]
    public void SetLearningGoal_rejects_an_undefined_value()
    {
        var profile = NewProfile();

        var act = () => profile.SetLearningGoal((LearningGoal)999);

        act.Should().Throw<DomainException>();
    }
}
