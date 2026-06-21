using Domain.Identity;
using FluentValidation;

namespace Application.Identity.UpdateProfile;

public sealed class UpdateProfileCommandValidator : AbstractValidator<UpdateProfileCommand>
{
    public UpdateProfileCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();

        RuleFor(x => x.DisplayName)
            .NotEmpty()
            .MaximumLength(200);

        RuleFor(x => x.Username)
            .NotEmpty()
            .Must(UsernameRules.IsValid)
            .WithMessage(
                $"Username must be {UsernameRules.MinLength}–{UsernameRules.MaxLength} characters " +
                "using only lowercase letters, digits, underscores and dots.");
    }
}
