using FluentValidation;

namespace Application.Vocabulary.Admin.DeleteVocabularyTopic;

public sealed class DeleteVocabularyTopicCommandValidator : AbstractValidator<DeleteVocabularyTopicCommand>
{
    public DeleteVocabularyTopicCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.Id).NotEmpty();
    }
}
