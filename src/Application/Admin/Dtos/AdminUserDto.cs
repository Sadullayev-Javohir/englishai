using Application.Identity.Dtos;

namespace Application.Admin.Dtos;

/// <summary>
/// One row of the admin panel's user list - everything the operator sees about a registered
/// account: identity, when they registered and last signed in, their admin role, learning
/// progress (onboarding, current CEFR level, last activity) and subscription standing. All
/// fields are read-only projections; the panel never edits a learner's data, only their admin
/// grant (via <see cref="SetUserAdmin.SetUserAdminCommand"/>).
/// </summary>
public sealed record AdminUserDto(
    Guid Id,
    string Email,
    string DisplayName,
    string? Username,
    string? PictureUrl,
    AdminRole Role,
    DateTimeOffset RegisteredAt,
    DateTimeOffset LastLoginAt,
    bool HasOnboarded,
    string? Level,
    DateTimeOffset? LastActivityAt,
    string SubscriptionStatus,
    string? SubscriptionPlan,
    DateTimeOffset? SubscriptionExpiresAt);
