using FluentValidation;

namespace Application.Vocabulary.Admin.GetAllVocabularyTopics;

public sealed class GetAllVocabularyTopicsQueryValidator : AbstractValidator<GetAllVocabularyTopicsQuery>
{
    public GetAllVocabularyTopicsQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
