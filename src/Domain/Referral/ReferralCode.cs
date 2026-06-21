using System.Security.Cryptography;

namespace Domain.Referral;

/// <summary>
/// Generates and validates the short, human-shareable referral codes each learner owns. Codes
/// use an unambiguous uppercase alphabet (no <c>0/O</c>, <c>1/I</c>) so they survive being read
/// aloud, typed on a phone, or pasted from a chat. Uniqueness is enforced by the persistence
/// adapter (a retry on collision), not here - this only produces a candidate and checks shape.
/// </summary>
public static class ReferralCode
{
    /// <summary>Unambiguous Crockford-style alphabet - excludes easily confused glyphs.</summary>
    private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

    /// <summary>How many characters a generated code has.</summary>
    public const int Length = 6;

    /// <summary>Produces a fresh random candidate code (uniqueness checked by the caller).</summary>
    public static string Generate()
    {
        Span<char> buffer = stackalloc char[Length];
        for (var i = 0; i < Length; i++)
            buffer[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];

        return new string(buffer);
    }

    /// <summary>Normalizes user input (trim + uppercase) so comparisons are case-insensitive.</summary>
    public static string Normalize(string code) => code?.Trim().ToUpperInvariant() ?? string.Empty;

    /// <summary>Whether a normalized value has the correct length and only allowed characters.</summary>
    public static bool IsValid(string code)
    {
        if (string.IsNullOrEmpty(code) || code.Length != Length)
            return false;

        foreach (var c in code)
            if (!Alphabet.Contains(c))
                return false;

        return true;
    }
}
