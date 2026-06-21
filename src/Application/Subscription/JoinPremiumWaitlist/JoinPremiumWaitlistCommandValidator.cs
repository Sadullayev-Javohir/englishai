using Domain.Subscription;
using FluentValidation;

namespace Application.Subscription.JoinPremiumWaitlist;

public sealed class JoinPremiumWaitlistCommandValidator : AbstractValidator<JoinPremiumWaitlistCommand>
{
    public JoinPremiumWaitlistCommandValidator()
    {
        // The rule itself stays in the domain, so another caller cannot bypass it. The validator
        // just runs it early, so a malformed contact comes back as a 400 the form can show against
        // the field rather than as a generic conflict.
        RuleFor(command => command.Contact)
            .NotEmpty()
            .MaximumLength(128)
            .Must(BeReachable)
            .WithMessage("Email manzili yoki O'zbekiston mobil raqamini kiriting.");

        RuleFor(command => command.Source)
            .MaximumLength(64);

        RuleFor(command => command.InterestedPlan)
            .IsInEnum()
            .When(command => command.InterestedPlan.HasValue);
    }

    private static bool BeReachable(string? contact)
    {
        if (string.IsNullOrWhiteSpace(contact))
            return false;

        try
        {
            PremiumWaitlistEntry.Normalize(contact);
            return true;
        }
        catch (Domain.Common.DomainException)
        {
            return false;
        }
    }
}
