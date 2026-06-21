using FluentValidation;

namespace Application.Vocabulary.GetVocabularyTopic;

public sealed class GetVocabularyTopicQueryValidator : AbstractValidator<GetVocabularyTopicQuery>
{
    public GetVocabularyTopicQueryValidator()
    {
        RuleFor(q => q.TopicId).NotEmpty();
    }
}
