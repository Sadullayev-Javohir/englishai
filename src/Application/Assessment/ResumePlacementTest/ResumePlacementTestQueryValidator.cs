using FluentValidation;

namespace Application.Assessment.ResumePlacementTest;

public sealed class ResumePlacementTestQueryValidator : AbstractValidator<ResumePlacementTestQuery>
{
    public ResumePlacementTestQueryValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
