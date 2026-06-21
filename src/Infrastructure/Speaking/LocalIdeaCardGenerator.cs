using Application.Speaking;
using Application.Speaking.Ports;
using DomainSpeaking = Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// Deterministic local stand-in for the LLM idea-card generator, so the "I'm stuck, give me an idea"
/// helper is exercisable and testable without an LLM API key. Returns the universal 5W1H/example
/// scaffolds (<see cref="IdeaCardDefaults"/>), which apply to any topic and level. Replace in
/// production with the budget-LLM adapter (<see cref="HermesIdeaCardGenerator"/>).
/// </summary>
public sealed class LocalIdeaCardGenerator : IIdeaCardGenerator
{
    public Task<IReadOnlyList<DomainSpeaking.IdeaCard>> GenerateAsync(
        DomainSpeaking.ConversationSession session,
        CancellationToken cancellationToken = default)
    {
        var subject = string.IsNullOrWhiteSpace(session.Topic)
            ? "this"
            : session.Topic.Replace('_', ' ');
        var starterSubject = string.IsNullOrWhiteSpace(session.Topic)
            ? "I would like to talk about"
            : $"For me, {subject}";
        var focusWord = session.FocusWords.FirstOrDefault();
        var cards = new List<DomainSpeaking.IdeaCard>
        {
            new($"What do you like most about {subject}?", $"What I like most is", "⭐"),
            new($"When or where do you usually experience {subject}?", "I usually", "📍"),
            new($"Can you give one simple example about {subject}?", "For example,", "✨"),
            new($"Why is {subject} important or interesting to you?", $"{starterSubject} is important because", "💭"),
        };

        if (!string.IsNullOrWhiteSpace(focusWord))
            cards[2] = new($"Can you make an example using the word '{focusWord}'?", $"I can use '{focusWord}' like this:", "🗣️");

        return Task.FromResult<IReadOnlyList<DomainSpeaking.IdeaCard>>(cards);
    }
}
