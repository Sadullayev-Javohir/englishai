using FluentValidation;

namespace Application.Video.OpenVideoByUrl;

public sealed class OpenVideoByUrlCommandValidator : AbstractValidator<OpenVideoByUrlCommand>
{
    public OpenVideoByUrlCommandValidator()
    {
        // YouTube video ids are 11 characters; allow a little slack for safety.
        RuleFor(x => x.YouTubeVideoId).NotEmpty().MaximumLength(20);
    }
}
