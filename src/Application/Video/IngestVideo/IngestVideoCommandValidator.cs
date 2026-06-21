using FluentValidation;

namespace Application.Video.IngestVideo;

public sealed class IngestVideoCommandValidator : AbstractValidator<IngestVideoCommand>
{
    public IngestVideoCommandValidator()
    {
        // YouTube video ids are 11 characters; allow a little slack for safety.
        RuleFor(x => x.YouTubeVideoId).NotEmpty().MaximumLength(20);
        RuleFor(x => x.Topic).NotEmpty().MaximumLength(60);
    }
}
