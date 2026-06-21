using Domain.Identity;
using FluentValidation;

namespace Application.Identity.SetPreferredName;

public sealed class SetPreferredNameCommandValidator : AbstractValidator<SetPreferredNameCommand>
{
    public SetPreferredNameCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        // A name the learner typed for the tutor to use - required and bounded. The domain trims
        // and enforces the same cap, so this gives the client a clean 400 instead of a 500.
        RuleFor(x => x.PreferredName)
            .NotEmpty()
            .MaximumLength(UserAccount.MaxPreferredNameLength);
    }
}
