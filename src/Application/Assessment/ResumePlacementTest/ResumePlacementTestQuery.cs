using Application.Assessment.Dtos;
using MediatR;

namespace Application.Assessment.ResumePlacementTest;

public sealed record ResumePlacementTestQuery(Guid SessionId)
    : IRequest<ResumePlacementTestResult>;

public sealed record ResumePlacementTestResult(
    Guid SessionId,
    bool IsCompleted,
    PlacementItemDto? CurrentItem);
