using Application.Learning.GetProgressInsight;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Learning;

public class ProgressInsightValidatorTests
{
    [Fact]
    public void Empty_learner_and_date_are_rejected()
    {
        var result = new GetProgressInsightQueryValidator().Validate(
            new GetProgressInsightQuery(Guid.Empty, default));

        result.Errors.Should().Contain(error => error.PropertyName == "LearnerId");
        result.Errors.Should().Contain(error => error.PropertyName == "Today");
    }

    [Fact]
    public void Populated_query_is_valid()
    {
        new GetProgressInsightQueryValidator().Validate(
            new GetProgressInsightQuery(Guid.NewGuid(), new DateOnly(2026, 7, 22)))
            .IsValid.Should().BeTrue();
    }
}
