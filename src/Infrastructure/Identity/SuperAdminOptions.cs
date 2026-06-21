namespace Infrastructure.Identity;

/// <summary>
/// Configures which accounts are super-admins of the operator panel. Mirrors
/// <see cref="ComplimentaryAccessOptions"/>: a built-in, always-on allowlist (so the grant survives a
/// missing/edited config file) merged with any extra emails supplied via the <c>SuperAdmin:Emails</c>
/// configuration section. The allowlist is matched against the account's verified, stored email, so it
/// cannot be spoofed (docs/development-guide.md §13). Super-admins are the only accounts that can promote other users
/// to admin.
/// </summary>
public sealed class SuperAdminOptions
{
    public const string SectionName = "SuperAdmin";

    /// <summary>
    /// Always-super-admin emails, independent of configuration. Plain emails (not secrets), so keeping
    /// them in code is consistent with docs/development-guide.md §13.
    /// </summary>
    public static readonly IReadOnlyList<string> BuiltInEmails = new[]
    {
        "javohirsadullayev836@gmail.com",
    };

    /// <summary>Additional super-admin emails supplied via configuration.</summary>
    public IReadOnlyList<string> Emails { get; set; } = Array.Empty<string>();
}
