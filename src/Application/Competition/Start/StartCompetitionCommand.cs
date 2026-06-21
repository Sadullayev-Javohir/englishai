using Application.Competition.Dtos;
using MediatR;

namespace Application.Competition.Start;

/// <summary>Host starts the competition; returns the full started view.</summary>
public sealed record StartCompetitionCommand(
    Guid CompetitionId,
    Guid HostLearnerId)
    : IRequest<CompetitionDto>;
