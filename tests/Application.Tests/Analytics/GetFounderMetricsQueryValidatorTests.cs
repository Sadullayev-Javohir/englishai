using Application.Analytics.GetFounderMetrics;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Analytics;

public class GetFounderMetricsQueryValidatorTests
{
    private readonly GetFounderMetricsQueryValidator _validator = new();

    [Fact]
    public void Accepts_a_90_day_inclusive_window()
    {
        var from = new DateOnly(2026, 5, 11);
        var result = _validator.Validate(new GetFounderMetricsQuery(Guid.NewGuid(), from, from.AddDays(89)));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void Rejects_a_window_longer_than_90_days()
    {
        var from = new DateOnly(2026, 5, 10);
        var result = _validator.Validate(new GetFounderMetricsQuery(Guid.NewGuid(), from, from.AddDays(90)));
        result.IsValid.Should().BeFalse();
    }
}
