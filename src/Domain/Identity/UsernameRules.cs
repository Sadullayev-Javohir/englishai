using System.Text.RegularExpressions;

namespace Domain.Identity;

/// <summary>
/// The single source of truth for what makes a valid handle/username. Shared by the domain
/// (when an account sets one), the Application validators, and - mirrored - the SPA, so the
/// rules can never drift between layers. A username is a public handle: lowercase latin
/// letters, digits, underscores and dots, 3–30 characters. Uniqueness is case-insensitive,
/// which is why <see cref="Normalize"/> lowercases before storing/comparing.
/// </summary>
public static partial class UsernameRules
{
    public const int MinLength = 3;
    public const int MaxLength = 30;

    [GeneratedRegex("^[a-z0-9_.]+$")]
    private static partial Regex AllowedChars();

    /// <summary>Trims and lowercases so storage and uniqueness checks are case-insensitive.</summary>
    public static string Normalize(string? username) =>
        (username ?? string.Empty).Trim().ToLowerInvariant();

    /// <summary>True when the (normalized) value satisfies length and character rules.</summary>
    public static bool IsValid(string? username)
    {
        var normalized = Normalize(username);
        return normalized.Length >= MinLength
               && normalized.Length <= MaxLength
               && AllowedChars().IsMatch(normalized);
    }
}
