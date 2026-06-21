using FluentValidation;

namespace Application.Video.GetRealtimeTranscript;

public sealed class GetRealtimeTranscriptQueryValidator : AbstractValidator<GetRealtimeTranscriptQuery>
{
    public GetRealtimeTranscriptQueryValidator()
    {
        // YouTube video ids are 11 characters; allow a little slack for safety.
        RuleFor(x => x.YouTubeVideoId).NotEmpty().MaximumLength(20);
    }
}
