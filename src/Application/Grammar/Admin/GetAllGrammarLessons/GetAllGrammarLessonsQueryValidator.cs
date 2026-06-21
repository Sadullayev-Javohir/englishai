using FluentValidation;

namespace Application.Grammar.Admin.GetAllGrammarLessons;

public sealed class GetAllGrammarLessonsQueryValidator : AbstractValidator<GetAllGrammarLessonsQuery>
{
    public GetAllGrammarLessonsQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
