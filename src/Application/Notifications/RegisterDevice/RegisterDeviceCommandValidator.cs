using FluentValidation;

namespace Application.Notifications.RegisterDevice;

public sealed class RegisterDeviceCommandValidator : AbstractValidator<RegisterDeviceCommand>
{
    private static readonly string[] AllowedPlatforms = { "android", "ios", "web" };

    public RegisterDeviceCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty().MaximumLength(4096);
        RuleFor(x => x.Platform)
            .NotEmpty()
            .Must(p => AllowedPlatforms.Contains(p.Trim().ToLowerInvariant()))
            .WithMessage("Platform must be one of: android, ios, web.");
    }
}
