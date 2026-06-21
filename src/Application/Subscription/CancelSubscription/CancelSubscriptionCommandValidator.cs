using FluentValidation;

namespace Application.Subscription.CancelSubscription;

public sealed class CancelSubscriptionCommandValidator : AbstractValidator<CancelSubscriptionCommand>
{
    public CancelSubscriptionCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
