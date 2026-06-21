using FluentValidation;

namespace Application.Subscription.StartSubscription;

public sealed class StartSubscriptionCommandValidator : AbstractValidator<StartSubscriptionCommand>
{
    public StartSubscriptionCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Plan).IsInEnum();
        RuleFor(x => x.Provider).IsInEnum();
        RuleFor(x => x.ReturnUrl).NotEmpty();
        RuleFor(x => x.WebhookBaseUrl).NotEmpty();
    }
}
