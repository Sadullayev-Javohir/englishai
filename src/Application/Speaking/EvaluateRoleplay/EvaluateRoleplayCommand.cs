using Application.Speaking.Dtos;
using MediatR;

namespace Application.Speaking.EvaluateRoleplay;

/// <summary>
/// Ends a roleplay sitting and scores it: sends the transcript through the roleplay evaluator and
/// returns a 0-100 score per dimension plus the Uzbek summary/strength/tip (from vetted templates -
/// docs/development-guide.md rule 11). Called when the learner taps "Finish" on the roleplay screen.
/// </summary>
public sealed record EvaluateRoleplayCommand(Guid SessionId)
    : IRequest<RoleplayEvaluationResult>;
