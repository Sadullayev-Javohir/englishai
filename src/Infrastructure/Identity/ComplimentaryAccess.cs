using Application.Common;
using Application.Identity.Ports;

namespace Infrastructure.Identity;

/// <summary>
/// Email-allowlist implementation of <see cref="IComplimentaryAccess"/>. It resolves the
/// learner's account from the store and matches its persisted (Google-verified) email against
/// the allowlist, case-insensitively. Because the email comes from the stored account - never
/// from the request - the only way onto the allowlist is to actually own the allowlisted Google
/// account, so another learner cannot tamper their way to free access (docs/development-guide.md §13).
/// </summary>
public sealed class ComplimentaryAccess : IComplimentaryAccess
{
    private readonly IUserAccountStore _accounts;
    private readonly IReadOnlySet<string> _allowlist;

    public ComplimentaryAccess(IUserAccountStore accounts, ComplimentaryAccessOptions options)
    {
        _accounts = accounts;
        _allowlist = ComplimentaryAccessOptions.BuiltInEmails
            .Concat(options.Emails ?? Array.Empty<string>())
            .Where(email => !string.IsNullOrWhiteSpace(email))
            .Select(Normalize)
            .ToHashSet();
    }

    public async Task<bool> HasFullAccessAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        if (_allowlist.Count == 0 || learnerId == Guid.Empty)
            return false;

        var account = await _accounts.GetByIdAsync(learnerId, cancellationToken);
        return account is not null && _allowlist.Contains(Normalize(account.Email));
    }

    private static string Normalize(string email) => email.Trim().ToLowerInvariant();
}
