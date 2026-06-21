using FluentValidation;

namespace Application.Identity.UpdateAvatar;

public sealed class UpdateAvatarCommandValidator : AbstractValidator<UpdateAvatarCommand>
{
    public const int MaxBytes = 5 * 1024 * 1024;

    public UpdateAvatarCommandValidator()
    {
        RuleFor(x => x.Data).NotEmpty().Must(data => data.Length <= MaxBytes)
            .WithMessage("Avatar image must not exceed 5 MB.");
        RuleFor(x => x.ContentType).Must(type => type is "image/jpeg" or "image/png" or "image/webp")
            .WithMessage("Avatar must be a JPEG, PNG, or WebP image.");
    }
}
