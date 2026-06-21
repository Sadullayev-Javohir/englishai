using FluentValidation;

namespace Application.Speaking.StartConversation;

public sealed class StartConversationCommandValidator : AbstractValidator<StartConversationCommand>
{
    private const int MaxTopicLength = 40;

    public StartConversationCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Level).IsInEnum();
        // Topic is an optional curated code, never free text - keep it short.
        RuleFor(x => x.Topic)
            .MaximumLength(MaxTopicLength)
            .When(x => x.Topic is not null);
        // When linking to a vocabulary topic, it must be a valid id (resolved server-side).
        RuleFor(x => x.VocabularyTopicId)
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage("VocabularyTopicId must be a valid GUID.")
            .When(x => x.VocabularyTopicId is not null);
    }
}
