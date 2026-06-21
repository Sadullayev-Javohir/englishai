using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// A single message in a speaking conversation. Learner turns may carry a
/// pronunciation assessment; tutor turns never do.
/// </summary>
public sealed class ConversationTurn
{
    private ConversationTurn(ConversationRole role, string text, PronunciationResult? pronunciation)
    {
        Role = role;
        Text = text;
        Pronunciation = pronunciation;
    }

    public ConversationRole Role { get; }
    public string Text { get; }
    public PronunciationResult? Pronunciation { get; }

    public static ConversationTurn Learner(string text, PronunciationResult? pronunciation = null)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Learner turn text must not be empty.");

        return new ConversationTurn(ConversationRole.Learner, text, pronunciation);
    }

    public static ConversationTurn Tutor(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Tutor turn text must not be empty.");

        return new ConversationTurn(ConversationRole.Tutor, text, pronunciation: null);
    }
}
