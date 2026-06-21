using FluentValidation;

namespace Application.Grammar.GetGrammarLesson;

public sealed class GetGrammarLessonQueryValidator : AbstractValidator<GetGrammarLessonQuery>
{
    public GetGrammarLessonQueryValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
    }
}
