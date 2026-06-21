using FluentValidation;

namespace Application.Video.ExplainSegment;

public sealed class ExplainSegmentQueryValidator : AbstractValidator<ExplainSegmentQuery>
{
    /// <summary>Upper bound on the current transcript line/focus; the full transcript stays server-side.</summary>
    public const int MaxFocusLength = 800;

    /// <summary>Upper bound on one learner question - keeps the LLM call small (rule 10).</summary>
    public const int MaxMessageLength = 300;

    public ExplainSegmentQueryValidator()
    {
        RuleFor(x => x.VideoLessonId).NotEmpty();
        RuleFor(x => x.FocusText).MaximumLength(MaxFocusLength);
        RuleFor(x => x.UserMessage).NotEmpty().MaximumLength(MaxMessageLength);
        RuleFor(x => x.History).NotNull();
    }
}
