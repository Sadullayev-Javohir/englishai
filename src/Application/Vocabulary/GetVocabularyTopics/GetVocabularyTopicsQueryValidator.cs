using FluentValidation;

namespace Application.Vocabulary.GetVocabularyTopics;

public sealed class GetVocabularyTopicsQueryValidator : AbstractValidator<GetVocabularyTopicsQuery>
{
    public GetVocabularyTopicsQueryValidator()
    {
        RuleFor(q => q.LearnerId).NotEmpty();
        RuleFor(q => q.Level).IsInEnum().When(q => q.Level.HasValue);
    }
}
