using Application.Competition.Dtos;
using MediatR;

namespace Application.Competition.Advance;

/// <summary>Host advances to the next slide (or finishes when exhausted); returns the view.</summary>
public sealed record AdvanceSlideCommand(
    Guid CompetitionId,
    Guid HostLearnerId)
    : IRequest<CompetitionDto>;
