using Domain.Speaking;

namespace Application.Speaking;

/// <summary>
/// The universal fallback set of idea cards, based on the classic 5W1H + example/opinion expansion
/// scaffolds that work for ANY topic and level. Used by the Local (no-LLM) generator, and by the
/// query handler whenever the LLM produced nothing usable or the session could not be found - so the
/// learner tapping "I need an idea" during a freeze is NEVER met with an error or an empty sheet.
/// All content is English (the target language); the Uzbek framing lives in the frontend content store.
/// </summary>
public static class IdeaCardDefaults
{
    public static IReadOnlyList<IdeaCard> Cards { get; } = new[]
    {
        new IdeaCard("Who is part of this? Who do you do it with?", "The people I want to talk about are", "👥"),
        new IdeaCard("When and where does it usually happen?", "It usually happens", "📍"),
        new IdeaCard("Why do you like it or not like it?", "I feel this way because", "💭"),
        new IdeaCard("Can you give one small example?", "For example,", "✨"),
    };
}
