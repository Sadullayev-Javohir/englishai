using FluentValidation;

namespace Application.Video.GetVideoLesson;

public sealed class GetVideoLessonQueryValidator : AbstractValidator<GetVideoLessonQuery>
{
    public GetVideoLessonQueryValidator()
    {
        RuleFor(x => x.VideoLessonId).NotEmpty();
    }
}
