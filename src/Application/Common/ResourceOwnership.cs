namespace Application.Common;

/// <summary>Shared object-level authorization checks for resources resolved by opaque ids.</summary>
public static class ResourceOwnership
{
    public static void EnsureCurrentLearner(ICurrentUserAccessor? currentUser, Guid resourceLearnerId)
    {
        // Unit/offline callers may not have an HTTP identity accessor. Production DI always
        // provides it; when present, an authenticated caller may only touch their own resource.
        var caller = currentUser?.LearnerId;
        if (caller is not null && caller.Value != resourceLearnerId)
            throw new ForbiddenException("You can only access your own data.");
    }

    public static Guid RequireCurrentLearner(ICurrentUserAccessor currentUser) =>
        currentUser.LearnerId
        ?? throw new ForbiddenException("An authenticated learner is required.");
}
