using MediatR;

namespace Application.Assessment.ReportIntegrityViolation;

public sealed record ReportPlacementIntegrityViolationCommand(
    Guid SessionId,
    Guid IncidentId,
    string Reason) : IRequest<ReportPlacementIntegrityViolationResult>;

/// <summary>
/// <paramref name="Stage"/>, <paramref name="ItemNumber"/> and <paramref name="TotalItems"/> are
/// diagnostics: the API logs them so it is visible which question the integrity rules fire on.
/// They are safe to expose - the client already renders the same stage and progress counter.
/// </summary>
public sealed record ReportPlacementIntegrityViolationResult(
    int ViolationCount,
    bool Invalidated,
    string Stage,
    int ItemNumber,
    int TotalItems);
