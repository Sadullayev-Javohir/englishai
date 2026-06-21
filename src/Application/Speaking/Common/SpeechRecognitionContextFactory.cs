using Application.Speaking.Ports;
using Domain.Speaking;

namespace Application.Speaking.Common;

public static class SpeechRecognitionContextFactory
{
    private static readonly string[] NamePromptMarkers =
    {
        "your name",
        "call you",
        "spell your name",
        "may i have your name",
        "who are you",
    };

    public static SpeechRecognitionContext FromSession(ConversationSession session)
    {
        var lastTutorPrompt = session.Turns
            .LastOrDefault(turn => turn.Role == ConversationRole.Tutor)?.Text;
        var contextPhrases = new List<string>();

        Add(contextPhrases, session.Topic);
        Add(contextPhrases, lastTutorPrompt);
        contextPhrases.AddRange(session.FocusWords);

        if (session.CurriculumContext is { } curriculum)
        {
            Add(contextPhrases, curriculum.TopicTitle);
            Add(contextPhrases, curriculum.Objective);
            Add(contextPhrases, curriculum.PrimaryGrammarFocus);
            Add(contextPhrases, curriculum.ReviewGrammarFocus);
            contextPhrases.AddRange(curriculum.PriorityWords);
            contextPhrases.AddRange(curriculum.Questions);
        }

        var expectsName = ContainsNamePrompt(lastTutorPrompt);
        if (session.ScenarioCode is { } scenarioCode && RoleplayScenarioCatalog.Contains(scenarioCode))
        {
            var scenario = RoleplayScenarioCatalog.Get(scenarioCode);
            Add(contextPhrases, scenario.EnglishTitle);
            Add(contextPhrases, scenario.Setting);
            Add(contextPhrases, scenario.LearnerObjective);
            expectsName |= session.LearnerTurnCount == 0
                && scenario.LearnerObjective.Contains("name", StringComparison.OrdinalIgnoreCase);
        }

        return new SpeechRecognitionContext(
            lastTutorPrompt,
            session.Topic,
            session.FocusWords,
            contextPhrases
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray(),
            IncludeNameHints: string.IsNullOrWhiteSpace(session.LearnerName) && expectsName);
    }

    private static bool ContainsNamePrompt(string? prompt) =>
        !string.IsNullOrWhiteSpace(prompt)
        && NamePromptMarkers.Any(marker => prompt.Contains(marker, StringComparison.OrdinalIgnoreCase));

    private static void Add(ICollection<string> values, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value)) values.Add(value);
    }
}
