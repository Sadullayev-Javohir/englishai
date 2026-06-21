using FluentValidation;

namespace Application.Retention.EvaluateChurnRisk;

public sealed class EvaluateChurnRiskQueryValidator : AbstractValidator<EvaluateChurnRiskQuery>
{
    public EvaluateChurnRiskQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
