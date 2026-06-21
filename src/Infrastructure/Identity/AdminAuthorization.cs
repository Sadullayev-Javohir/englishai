using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;

namespace Infrastructure.Identity;

/// <summary>
/// Email-allowlist implementation of <see cref="IAdminAuthorization"/>. The super-admin set comes
/// from <see cref="SuperAdminOptions"/> (built-in + config), matched case-insensitively against the
/// account's persisted, Google-verified email - never anything from the request - so the role cannot
/// be tampered with (docs/development-guide.md §13). An ordinary admin is any account the super-admin has flagged
/// <see cref="Domain.Identity.UserAccount.IsAdmin"/>.
/// </summary>
public sealed class AdminAuthorization : IAdminAuthorization
{
    private readonly IUserAccountStore _accounts;
    private readonly IReadOnlySet<string> _superAdmins;

    public AdminAuthorization(IUserAccountStore accounts, SuperAdminOptions options)
    {
        _accounts = accounts;
        _superAdmins = SuperAdminOptions.BuiltInEmails
            .Concat(options.Emails ?? Array.Empty<string>())
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(Normalize)
            .ToHashSet();
    }

    public async Task<AdminRole> GetRoleAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
            return AdminRole.None;

        var account = await _accounts.GetByIdAsync(userId, cancellationToken);
        if (account is null)
            return AdminRole.None;

        if (IsSuperAdminEmail(account.Email))
            return AdminRole.SuperAdmin;

        return account.IsAdmin ? AdminRole.Admin : AdminRole.None;
    }

    public bool IsSuperAdminEmail(string email) =>
        !string.IsNullOrWhiteSpace(email) && _superAdmins.Contains(Normalize(email));

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
