using FluentValidation;

namespace Application.Identity.DeleteAccount;

public sealed class DeleteAccountCommandValidator : AbstractValidator<DeleteAccountCommand>
{
    public DeleteAccountCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.ConfirmationEmail)
            .NotEmpty()
            .EmailAddress();
    }
}
