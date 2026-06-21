using System.Collections.Concurrent;
using System.Reflection;
using MediatR;

namespace Application.Common;

/// <summary>
/// Enforces that an authenticated caller may only act on their own learner data. Every
/// learner-scoped request in this codebase carries the owner as a <c>Guid LearnerId</c>
/// property (a uniform convention); this behavior compares that value against the
/// authenticated learner (the JWT <c>sub</c>) and rejects mismatches with a 403.
///
/// When there is no authenticated user (background jobs, anonymous/test hosts), ownership
/// is not enforced - those paths are gated by other means. Requests without a
/// <c>LearnerId</c> property (resource-by-id endpoints, auth) are unaffected.
/// </summary>
public sealed class LearnerOwnershipBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo?> LearnerIdProperties = new();

    private readonly ICurrentUserAccessor _currentUser;

    public LearnerOwnershipBehavior(ICurrentUserAccessor currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<TResponse> Handle(
        TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        if (OwnershipBypass.IsActive
            || request is IOwnershipExempt
            || typeof(TRequest).Namespace?.StartsWith("Application.Admin", StringComparison.Ordinal) == true)
            return next();

        var current = _currentUser.LearnerId;
        if (current is not null)
        {
            var property = LearnerIdProperties.GetOrAdd(typeof(TRequest), static type =>
            {
                var prop = type.GetProperty("LearnerId");
                return prop is not null && prop.PropertyType == typeof(Guid) ? prop : null;
            });

            if (property is not null)
            {
                var requested = (Guid)property.GetValue(request)!;
                // Empty means "not supplied" (server fills it elsewhere); only a populated,
                // mismatched id is an attempt to reach another learner's data.
                if (requested != Guid.Empty && requested != current.Value)
                    throw new ForbiddenException("You can only access your own data.");
            }
        }

        return next();
    }
}
