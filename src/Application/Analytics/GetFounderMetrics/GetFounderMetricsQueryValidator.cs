using FluentValidation;

namespace Application.Analytics.GetFounderMetrics;

public sealed class GetFounderMetricsQueryValidator : AbstractValidator<GetFounderMetricsQuery>
{
    public GetFounderMetricsQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.From <= x.To)
            .WithMessage("From date must not be after to date.");
        RuleFor(x => x)
            .Must(x => x.From is null || x.To is null || x.To.Value.DayNumber - x.From.Value.DayNumber < 90)
            .WithMessage("Metrics date range cannot exceed 90 days.");
    }
}
