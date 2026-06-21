using FluentValidation;

namespace Application.Video.RateVideoDifficulty;

public sealed class RateVideoDifficultyCommandValidator : AbstractValidator<RateVideoDifficultyCommand>
{
    public RateVideoDifficultyCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.VideoLessonId).NotEmpty();
        RuleFor(x => x.Rating).IsInEnum();
    }
}
