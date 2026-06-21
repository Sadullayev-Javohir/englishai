using Application.Assessment.Dtos;
using MediatR;

namespace Application.Assessment.FinalizePlacementTest;

/// <summary>
/// Finalizes a placement test and returns the overall CEFR level plus per-stage
/// sub-levels. If the learner ended early (e.g. skipped the optional Speaking
/// stage), the session is completed first and scored from the answers recorded.
/// </summary>
public sealed record FinalizePlacementTestCommand(Guid SessionId) : IRequest<PlacementResultDto>;
