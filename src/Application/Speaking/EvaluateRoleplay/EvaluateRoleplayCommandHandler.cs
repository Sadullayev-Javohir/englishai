using Application.Ai;
using Application.Common;
using Application.Speaking.Dtos;
using Application.Speaking.Ports;
using Domain.Speaking;
using MediatR;

namespace Application.Speaking.EvaluateRoleplay;

public sealed class EvaluateRoleplayCommandHandler
    : IRequestHandler<EvaluateRoleplayCommand, RoleplayEvaluationResult>
{
    private readonly IConversationStore _conversations;
    private readonly IRoleplayEvaluator _evaluator;
    private readonly IFeedbackTemplateProvider _feedback;
    private readonly ICurrentUserAccessor? _currentUser;
    private readonly IAiFeatureScope _aiScope;

    public EvaluateRoleplayCommandHandler(
        IConversationStore conversations,
        IRoleplayEvaluator evaluator,
        IFeedbackTemplateProvider feedback,
        ICurrentUserAccessor? currentUser = null,
        IAiFeatureScope? aiScope = null)
    {
        _conversations = conversations;
        _evaluator = evaluator;
        _feedback = feedback;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
        _currentUser = currentUser;
    }

    public async Task<RoleplayEvaluationResult> Handle(
        EvaluateRoleplayCommand request, CancellationToken cancellationToken)
    {
        var session = await _conversations.GetAsync(request.SessionId, cancellationToken)
            ?? throw new NotFoundException(nameof(ConversationSession), request.SessionId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, session.LearnerId);

        if (session.ScenarioCode is not { } scenarioCode)
            throw new ConflictException("This conversation is not a roleplay and cannot be scored.");

        var definition = RoleplayScenarioCatalog.Get(scenarioCode);

        // Nothing to grade: the learner never actually spoke. Return a non-evaluable result telling
        // them to speak first, rather than fabricating scores from an empty transcript.
        if (session.LearnerTurnCount == 0)
        {
            return new RoleplayEvaluationResult(
                Evaluable: false,
                scenarioCode,
                OverallScore: 0,
                TaskCompletion: 0,
                Fluency: 0,
                Grammar: 0,
                Appropriateness: 0,
                Band: PronunciationBand.NeedsImprovement,
                StrongestDimension: RoleplayDimension.TaskCompletion,
                WeakestDimension: RoleplayDimension.TaskCompletion,
                SummaryUz: _feedback.Get("roleplay.too_short"),
                StrengthUz: null,
                TipUz: null);
        }

        using var aiScope = await _aiScope.EnterAsync(AiFeature.SpeakingEvaluation, session.LearnerId, cancellationToken);
        var evaluation = await _evaluator.EvaluateAsync(session, definition, cancellationToken);

        // All learner-facing Uzbek text comes from vetted templates (docs/development-guide.md rule 11): the overall
        // line by band, praise for the strongest dimension, and a tip for the weakest one.
        var summaryCode = evaluation.Band == PronunciationBand.Good
            ? "roleplay.summary.good"
            : "roleplay.summary.needs_improvement";
        var strengthUz = _feedback.Get($"roleplay.strength.{DimensionCode(evaluation.StrongestDimension)}");
        var tipUz = _feedback.Get($"roleplay.tip.{DimensionCode(evaluation.WeakestDimension)}");

        return new RoleplayEvaluationResult(
            Evaluable: true,
            scenarioCode,
            evaluation.OverallScore,
            Math.Round(evaluation.TaskCompletion, 1),
            Math.Round(evaluation.Fluency, 1),
            Math.Round(evaluation.Grammar, 1),
            Math.Round(evaluation.Appropriateness, 1),
            evaluation.Band,
            evaluation.StrongestDimension,
            evaluation.WeakestDimension,
            _feedback.Get(summaryCode),
            strengthUz,
            tipUz);
    }

    private static string DimensionCode(RoleplayDimension dimension) => dimension switch
    {
        RoleplayDimension.TaskCompletion => "task_completion",
        RoleplayDimension.Fluency => "fluency",
        RoleplayDimension.Grammar => "grammar",
        RoleplayDimension.Appropriateness => "appropriateness",
        _ => "task_completion"
    };
}
