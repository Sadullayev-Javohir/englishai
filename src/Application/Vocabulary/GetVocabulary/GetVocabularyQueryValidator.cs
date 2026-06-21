using FluentValidation;

namespace Application.Vocabulary.GetVocabulary;

public sealed class GetVocabularyQueryValidator : AbstractValidator<GetVocabularyQuery>
{
    public GetVocabularyQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
