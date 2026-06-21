using FluentValidation;

namespace Application.Subscription.CheckFeatureAccess;

public sealed class CheckFeatureAccessQueryValidator : AbstractValidator<CheckFeatureAccessQuery>
{
    public CheckFeatureAccessQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Feature).IsInEnum();
    }
}
