using FluentValidation;

namespace Application.Identity.CheckUsernameAvailability;

public sealed class CheckUsernameAvailabilityQueryValidator
    : AbstractValidator<CheckUsernameAvailabilityQuery>
{
    public CheckUsernameAvailabilityQueryValidator()
    {
        // Format validity is reported in the result (not as a 400) so the live check can tell
        // the user *why* it is unavailable; we only guard against an empty probe here.
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.ExcludingUserId).NotEmpty();
    }
}
