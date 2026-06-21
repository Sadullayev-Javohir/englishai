using Application.Identity.Dtos;

namespace Application.Common;

/// <summary>
/// Resolves an account's <see cref="AdminRole"/> for the operator admin panel. The super-admin is
/// identified by a built-in, verified-email allowlist (resolved server-side from the stored account,
/// never from the request - like <see cref="IComplimentaryAccess"/>), so it cannot be spoofed and the
/// grant survives any data edit. Ordinary admins are accounts the super-admin has promoted
/// (<see cref="Domain.Identity.UserAccount.IsAdmin"/>). Used both to gate the admin endpoints and to
/// label each user in the panel (docs/development-guide.md §13).
/// </summary>
public interface IAdminAuthorization
{
    /// <summary>
    /// The effective role of the account: <see cref="AdminRole.SuperAdmin"/> when its email is on the
    /// super-admin allowlist, otherwise <see cref="AdminRole.Admin"/> when it has been granted admin,
    /// otherwise <see cref="AdminRole.None"/>. Returns <see cref="AdminRole.None"/> for unknown ids.
    /// </summary>
    Task<AdminRole> GetRoleAsync(Guid userId, CancellationToken cancellationToken);

    /// <summary>
    /// Whether a (verified, stored) email is on the super-admin allowlist. Lets a caller that already
    /// holds the loaded accounts label them without an extra per-account round-trip.
    /// </summary>
    bool IsSuperAdminEmail(string email);
}
