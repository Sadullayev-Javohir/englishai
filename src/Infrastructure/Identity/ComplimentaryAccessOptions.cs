namespace Infrastructure.Identity;

/// <summary>
/// Configures which accounts receive complimentary full access (see
/// <see cref="Application.Common.IComplimentaryAccess"/>). The allowlist is a list of emails;
/// extra entries can be supplied through the <c>ComplimentaryAccess:Emails</c> configuration
/// section, and they are merged with the always-on <see cref="BuiltInEmails"/>.
/// </summary>
public sealed class ComplimentaryAccessOptions
{
    public const string SectionName = "ComplimentaryAccess";

    /// <summary>
    /// Accounts that are always comped, independent of configuration, so the grant survives a
    /// missing/edited config file. These are plain emails (not secrets), so keeping them in code
    /// is consistent with docs/development-guide.md §13.
    /// </summary>
    public static readonly IReadOnlyList<string> BuiltInEmails = new[]
    {
        "javohirsadullayev836@gmail.com",
    };

    /// <summary>Additional comped emails supplied via configuration.</summary>
    public IReadOnlyList<string> Emails { get; set; } = Array.Empty<string>();
}
