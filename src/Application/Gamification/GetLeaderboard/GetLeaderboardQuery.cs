using Application.Gamification.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Gamification.GetLeaderboard;

/// <summary>
/// Reads the top 50 of a CEFR level's leaderboard plus the learner's own rank (leaderboard/
/// points feature). When <paramref name="Level"/> is omitted, the learner's own current CEFR
/// level is used, so opening the page without a tab selection shows "your" board.
/// </summary>
public sealed record GetLeaderboardQuery(Guid LearnerId, CefrLevel? Level) : IRequest<LeaderboardDto>;
