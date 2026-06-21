using FluentValidation;

namespace Application.Listening.GetListeningExercise;

public sealed class GetListeningExerciseQueryValidator : AbstractValidator<GetListeningExerciseQuery>
{
    public GetListeningExerciseQueryValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
    }
}
