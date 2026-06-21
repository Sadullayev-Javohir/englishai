using Application.Speaking.Ports;
using Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// Deterministic local stand-in for the LLM roleplay evaluator, so the roleplay flow is exercisable
/// and testable without an LLM API key. Derives plausible, repeatable dimension scores from simple
/// transcript signals (how many turns the learner took and how much they said). Replace in production
/// with the budget-LLM adapter (<see cref="HermesRoleplayEvaluator"/>).
/// </summary>
public sealed class LocalRoleplayEvaluator : IRoleplayEvaluator
{
    public Task<RoleplayEvaluation> EvaluateAsync(
        ConversationSession session,
        RoleplayScenarioDefinition scenario,
        CancellationToken cancellationToken = default)
    {
        var learnerTurns = session.Turns
            .Where(t => t.Role == ConversationRole.Learner)
            .ToList();

        var turnCount = learnerTurns.Count;
        var totalWords = learnerTurns.Sum(t => WordCount(t.Text));
        var averageWords = turnCount == 0 ? 0 : (double)totalWords / turnCount;

        // Task completion grows with how much the learner engaged (each exchange advances the scene),
        // saturating at five turns. Fluency reflects how much they produced per turn. Grammar and
        // appropriateness are steady baselines nudged by overall engagement. All clamped to 0-100.
        var taskCompletion = Clamp(40 + turnCount * 12);
        var fluency = Clamp(45 + averageWords * 5);
        var grammar = Clamp(60 + turnCount * 3);
        var appropriateness = Clamp(65 + turnCount * 2);

        return Task.FromResult(new RoleplayEvaluation(taskCompletion, fluency, grammar, appropriateness));
    }

    private static int WordCount(string text) =>
        text.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries).Length;

    private static double Clamp(double value) => Math.Clamp(Math.Round(value, 1), 0, 100);
}
