using FluentValidation;

namespace Application.Levels.GetLevelMap;

public sealed class GetLevelMapQueryValidator : AbstractValidator<GetLevelMapQuery>
{
    public GetLevelMapQueryValidator()
    {
        RuleFor(q => q.LearnerId).NotEmpty();
        RuleFor(q => q.Level).IsInEnum().When(q => q.Level.HasValue);
    }
}
