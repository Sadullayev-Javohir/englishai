using Application.Competition.Dtos;
using Domain.Competition;
using MediatR;

namespace Application.Competition.Create;

/// <summary>Creates a competition, builds its slide sequence from the selected filled topics,
/// and returns the view with the host-only access code.</summary>
public sealed record CreateCompetitionCommand(
    Guid HostLearnerId,
    string HostDisplayName,
    string Title,
    CompetitionSettings Settings,
    List<Guid> TopicIds)
    : IRequest<CompetitionDto>;
