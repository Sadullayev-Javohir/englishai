using Domain.Speaking;

namespace Application.Speaking.Ports;

/// <summary>
/// Produces a small set of "idea cards" that help a learner who has run out of things to say in a
/// live conversation - concrete, answerable talking points tailored to the current session's topic,
/// level and recent turns. Like the other AI ports it must use a budget model, cap output tokens and
/// return only English learning content (never free Uzbek prose - docs/development-guide.md rules 10, 11). A
/// deterministic Local implementation lets the app run and be tested without an LLM key.
/// </summary>
public interface IIdeaCardGenerator
{
    Task<IReadOnlyList<IdeaCard>> GenerateAsync(
        ConversationSession session,
        CancellationToken cancellationToken = default);
}
