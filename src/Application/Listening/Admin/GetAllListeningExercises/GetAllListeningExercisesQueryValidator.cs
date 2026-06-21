using FluentValidation;

namespace Application.Listening.Admin.GetAllListeningExercises;

public sealed class GetAllListeningExercisesQueryValidator : AbstractValidator<GetAllListeningExercisesQuery>
{
    public GetAllListeningExercisesQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
