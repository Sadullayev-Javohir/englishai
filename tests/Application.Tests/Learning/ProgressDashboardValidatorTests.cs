using Application.Learning.GetProgressDashboard;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Learning;

public class ProgressDashboardValidatorTests
{
    [Fact]
    public void Empty_learner_and_date_are_rejected()
    {
        var result = new GetProgressDashboardQueryValidator().Validate(
            new GetProgressDashboardQuery(Guid.Empty, default));

        result.Errors.Should().Contain(error => error.PropertyName == "LearnerId");
        result.Errors.Should().Contain(error => error.PropertyName == "Today");
    }

    [Fact]
    public void Populated_query_is_valid()
    {
        var result = new GetProgressDashboardQueryValidator().Validate(
            new GetProgressDashboardQuery(Guid.NewGuid(), new DateOnly(2026, 7, 22)));

        result.IsValid.Should().BeTrue();
    }
}
