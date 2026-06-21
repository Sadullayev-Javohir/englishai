using Application.Learning.GetGrowth;
using Application.Learning.GetLearnerOverview;
using Application.Learning.RecordSkillActivity;
using Domain.Learning;
using FluentValidation.TestHelper;
using Xunit;

namespace Application.Tests.Learning;

public class LearningValidatorsTests
{
    [Fact]
    public void RecordActivity_validator_rejects_out_of_range_score()
    {
        new RecordSkillActivityCommandValidator()
            .TestValidate(new RecordSkillActivityCommand(Guid.NewGuid(), SkillType.Speaking, 150))
            .ShouldHaveValidationErrorFor(x => x.Score);
    }

    [Fact]
    public void RecordActivity_validator_rejects_empty_learner_id()
    {
        new RecordSkillActivityCommandValidator()
            .TestValidate(new RecordSkillActivityCommand(Guid.Empty, SkillType.Speaking, 50))
            .ShouldHaveValidationErrorFor(x => x.LearnerId);
    }

    [Fact]
    public void RecordActivity_validator_accepts_valid_command()
    {
        new RecordSkillActivityCommandValidator()
            .TestValidate(new RecordSkillActivityCommand(Guid.NewGuid(), SkillType.Grammar, 75))
            .ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Overview_validator_rejects_empty_learner_id()
    {
        new GetLearnerOverviewQueryValidator()
            .TestValidate(new GetLearnerOverviewQuery(Guid.Empty))
            .ShouldHaveValidationErrorFor(x => x.LearnerId);
    }

    [Fact]
    public void Growth_validator_rejects_zero_weeks()
    {
        new GetGrowthQueryValidator()
            .TestValidate(new GetGrowthQuery(Guid.NewGuid(), Weeks: 0))
            .ShouldHaveValidationErrorFor(x => x.Weeks);
    }
}
