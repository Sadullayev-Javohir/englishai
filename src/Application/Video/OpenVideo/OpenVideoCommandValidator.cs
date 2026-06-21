using FluentValidation;

namespace Application.Video.OpenVideo;

public sealed class OpenVideoCommandValidator : AbstractValidator<OpenVideoCommand>
{
    /// <summary>Upper bound (4 hours) guarding against a bogus duration being persisted.</summary>
    private const int MaxDurationSeconds = 4 * 60 * 60;

    public OpenVideoCommandValidator()
    {
        RuleFor(x => x.YouTubeVideoId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Title).NotEmpty().MaximumLength(300);
        RuleFor(x => x.Channel).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(60);
        RuleFor(x => x.DurationSeconds).InclusiveBetween(1, MaxDurationSeconds);
        RuleFor(x => x.Level).IsInEnum();
    }
}
