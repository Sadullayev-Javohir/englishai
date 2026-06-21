namespace Application.Common;

/// <summary>
/// Exposes the authenticated learner for the current request. Implemented in the Web layer
/// from the JWT <c>sub</c> claim. Returns <c>null</c> when there is no authenticated user
/// (e.g. background jobs, or anonymous/test hosts) - callers must treat null as "ownership
/// not enforced" rather than "denied".
/// </summary>
public interface ICurrentUserAccessor
{
    Guid? LearnerId { get; }
}
