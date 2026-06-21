using Domain.Speaking;

namespace Application.Speaking.Ports;

/// <summary>
/// Scores a finished roleplay sitting. Given the conversation transcript and the scenario the learner
/// was rehearsing, it returns a 0-100 score per <see cref="RoleplayDimension"/>. Like the other AI
/// ports it must use a budget model, cap output tokens and return a strictly structured result - never
/// user-facing Uzbek prose (docs/development-guide.md rules 10, 11); the Uzbek feedback is assembled from templates by
/// the handler. A deterministic Local implementation lets the app run and be tested without an LLM key.
/// </summary>
public interface IRoleplayEvaluator
{
    Task<RoleplayEvaluation> EvaluateAsync(
        ConversationSession session,
        RoleplayScenarioDefinition scenario,
        CancellationToken cancellationToken = default);
}
