using Application.Competition.Dtos;
using MediatR;

namespace Application.Competition.Join;

/// <summary>Joins (or creates-and-joins) a learner to a competition by access code and
/// returns the updated competition view.</summary>
public sealed record JoinCompetitionCommand(
    Guid CompetitionId,
    Guid LearnerId,
    string DisplayName,
    string? AccessCode)
    : IRequest<CompetitionDto>;
