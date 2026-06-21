using Application.Identity.Dtos;

namespace Application.Admin.Dtos;

/// <summary>
/// The admin panel's user-list payload: the viewer's own role (so the SPA shows the promote/demote
/// controls only to a super-admin), simple headline counts, and every user row. Returned only to an
/// authorized admin (the handler rejects non-admins with a 403).
/// </summary>
public sealed record AdminUsersDto(
    AdminRole ViewerRole,
    int TotalUsers,
    int AdminCount,
    int OnboardedCount,
    int PremiumCount,
    IReadOnlyList<AdminUserDto> Users,
    string? NextCursor = null);
