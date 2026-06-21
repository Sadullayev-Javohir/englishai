using FluentValidation;

namespace Application.Grammar.Admin.DeleteGrammarLesson;

public sealed class DeleteGrammarLessonCommandValidator : AbstractValidator<DeleteGrammarLessonCommand>
{
    public DeleteGrammarLessonCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
