using Application.Gamification.Dtos;
using MediatR;

namespace Application.Gamification.CompleteTask;

/// <summary>
/// Records that a learner finished one learning task today (a Speaking turn, an SRS
/// review, a finished video lesson, etc.). When today's count reaches the daily goal the
/// day is marked complete, which keeps/extends the streak (PROJECT-SPEC Faza 5). Returns
/// the updated daily-goal and streak status.
/// </summary>
public sealed record CompleteTaskCommand(Guid LearnerId) : IRequest<GamificationStatusDto>;
