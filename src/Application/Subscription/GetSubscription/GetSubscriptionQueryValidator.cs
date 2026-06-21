using FluentValidation;

namespace Application.Subscription.GetSubscription;

public sealed class GetSubscriptionQueryValidator : AbstractValidator<GetSubscriptionQuery>
{
    public GetSubscriptionQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
