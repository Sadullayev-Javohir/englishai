using Application.Competition.Dtos;
using MediatR;

namespace Application.Competition.Finish;

/// <summary>Host finishes the competition; returns the final results.</summary>
public sealed record FinishCompetitionCommand(
    Guid CompetitionId,
    Guid HostLearnerId)
    : IRequest<CompetitionResultDto>;
