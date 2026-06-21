using Application.Ai;
using Application.Assistant.Dtos;
using Application.Assistant.Ports;
using Application.Common;
using Application.Subscription.Entitlements;
using Domain.Subscription;
using MediatR;

namespace Application.Assistant.AskAssistant;

public sealed class AskAssistantQueryHandler : IRequestHandler<AskAssistantQuery, AssistantReplyDto>
{
    private const int MaxHistoryTurns = 6;
    private readonly ILearningAssistant _assistant;
    private readonly IAiFeatureScope _aiScope;
    private readonly ICurrentUserAccessor? _currentUser;
    private readonly IEntitlementService? _entitlements;

    public AskAssistantQueryHandler(
        ILearningAssistant assistant,
        IAiFeatureScope? aiScope = null,
        ICurrentUserAccessor? currentUser = null,
        IEntitlementService? entitlements = null)
    {
        _assistant = assistant;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
        _currentUser = currentUser;
        _entitlements = entitlements;
    }

    public async Task<AssistantReplyDto> Handle(
        AskAssistantQuery request,
        CancellationToken cancellationToken)
    {
        var history = request.History
            .TakeLast(MaxHistoryTurns)
            .Select(turn => new AssistantTurn(turn.Role.Trim().ToLowerInvariant(), turn.Text.Trim()))
            .ToArray();

        // Two LLM passes (a draft and a verification), each with the largest output budget in the
        // app - the single most expensive call a learner can trigger, and until now unmetered.
        var learnerId = _currentUser?.LearnerId;
        if (_entitlements is not null && learnerId is { } gatedLearner)
        {
            await _entitlements.EnsureAllowedAsync(
                gatedLearner, PremiumFeature.AssistantQuestion, cancellationToken);
        }

        using var scope = await _aiScope.EnterAsync(AiFeature.Assistant, learnerId, cancellationToken);
        var reply = await _assistant.AnswerAsync(request.Question.Trim(), history, cancellationToken);

        // Charged only for an answer the learner actually received: a failed or empty reply is our
        // problem, not theirs.
        if (_entitlements is not null && learnerId is { } chargedLearner && !string.IsNullOrWhiteSpace(reply))
        {
            await _entitlements.RecordUsageAsync(
                chargedLearner, PremiumFeature.AssistantQuestion, cancellationToken);
        }

        return new AssistantReplyDto(string.IsNullOrWhiteSpace(reply) ? null : reply.Trim());
    }
}
