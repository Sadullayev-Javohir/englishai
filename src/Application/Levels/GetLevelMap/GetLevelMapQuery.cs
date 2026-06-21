using Application.Levels.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Levels.GetLevelMap;

/// <summary>
/// Builds the Level Map for a CEFR level (PROJECT-SPEC M.3). When <paramref name="Level"/> is
/// omitted the learner's current level is used, so the home entry point lands on "where I am".
/// </summary>
public sealed record GetLevelMapQuery(Guid LearnerId, CefrLevel? Level = null)
    : IRequest<LevelMapDto>;
