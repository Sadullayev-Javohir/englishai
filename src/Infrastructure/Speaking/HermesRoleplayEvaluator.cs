using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Application.Speaking.Ports;
using Infrastructure.Llm;
using DomainSpeaking = Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// LLM-backed roleplay evaluator routed through the internal Hermes Agent Gateway. Uses a budget
/// model with a hard output-token cap. It returns ONLY a strict JSON object of four 0-100 scores -
/// never user-facing Uzbek prose (rule 11); the Uzbek summary is assembled from templates by the
/// handler. If the model returns anything unparseable, a neutral mid-scale evaluation is used so the
/// learner still gets a result.
/// </summary>
public sealed class HermesRoleplayEvaluator : IRoleplayEvaluator
{
    private readonly ILlmCompletion _completion;

    public HermesRoleplayEvaluator(ILlmCompletion completion)
    {
        _completion = completion;
    }

    public async Task<DomainSpeaking.RoleplayEvaluation> EvaluateAsync(
        DomainSpeaking.ConversationSession session,
        DomainSpeaking.RoleplayScenarioDefinition scenario,
        CancellationToken cancellationToken = default)
    {
        const double neutral = 60;
        var text = await _completion.CompleteAsync(
            InstructionPrompt, BuildEvaluationRequest(session, scenario), 400, cancellationToken);
        return string.IsNullOrWhiteSpace(text)
            ? new DomainSpeaking.RoleplayEvaluation(neutral, neutral, neutral, neutral)
            : ParseOrNeutral(text);
    }

    private const string InstructionPrompt =
        """
        You are an examiner scoring how an English learner performed in a role-play scene. You will be
        given the scenario, the learner's goal, and the full transcript. Judge only the learner's turns.

        Score the learner from 0 to 100 on each of these four dimensions:
        - taskCompletion: did they accomplish the goal of the scene?
        - fluency: how smoothly and confidently did they communicate?
        - grammar: grammatical accuracy and range of vocabulary.
        - appropriateness: was their register and politeness suitable for the situation?

        Respond with ONLY a compact JSON object and nothing else, in exactly this shape:
        {"taskCompletion": 0, "fluency": 0, "grammar": 0, "appropriateness": 0}
        Do not add any explanation, markdown, or extra text.
        """;

    private static string BuildEvaluationRequest(
        DomainSpeaking.ConversationSession session,
        DomainSpeaking.RoleplayScenarioDefinition scenario)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Scenario: {scenario.EnglishTitle}");
        sb.AppendLine($"The learner played the customer/candidate; the other speaker was {scenario.PersonaRole}.");
        sb.AppendLine($"The learner's goal was to {scenario.LearnerObjective}.");
        sb.AppendLine($"Learner CEFR level: {session.Level}.");
        sb.AppendLine();
        sb.AppendLine("Transcript:");
        foreach (var turn in session.Turns)
        {
            var speaker = turn.Role == DomainSpeaking.ConversationRole.Learner ? "Learner" : "Other";
            sb.AppendLine($"{speaker}: {turn.Text}");
        }

        return sb.ToString();
    }

    private static DomainSpeaking.RoleplayEvaluation ParseOrNeutral(string text)
    {
        const double neutral = 60;
        try
        {
            var start = text.IndexOf('{');
            var end = text.LastIndexOf('}');
            if (start < 0 || end <= start)
                return new DomainSpeaking.RoleplayEvaluation(neutral, neutral, neutral, neutral);

            var json = text.Substring(start, end - start + 1);
            var scores = JsonSerializer.Deserialize<Scores>(json);
            if (scores is null)
                return new DomainSpeaking.RoleplayEvaluation(neutral, neutral, neutral, neutral);

            return new DomainSpeaking.RoleplayEvaluation(
                Clamp(scores.TaskCompletion),
                Clamp(scores.Fluency),
                Clamp(scores.Grammar),
                Clamp(scores.Appropriateness));
        }
        catch (JsonException)
        {
            return new DomainSpeaking.RoleplayEvaluation(neutral, neutral, neutral, neutral);
        }
    }

    private static double Clamp(double value) => Math.Clamp(value, 0, 100);

    private sealed record Scores(
        [property: JsonPropertyName("taskCompletion")] double TaskCompletion,
        [property: JsonPropertyName("fluency")] double Fluency,
        [property: JsonPropertyName("grammar")] double Grammar,
        [property: JsonPropertyName("appropriateness")] double Appropriateness);
}
