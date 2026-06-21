using FluentValidation;

namespace Application.Identity.AuthenticateWithGoogle;

public sealed class AuthenticateWithGoogleCommandValidator : AbstractValidator<AuthenticateWithGoogleCommand>
{
    public AuthenticateWithGoogleCommandValidator()
    {
        RuleFor(x => x.IdToken).NotEmpty();
    }
}
