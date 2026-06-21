using System.Security.Cryptography;
using System.Text;
using Application.Developer.Ports;
using Domain.Developer;

namespace Infrastructure.Developer;

public sealed class DeveloperApiKeyProtector : IDeveloperApiKeyProtector
{
    private const string KeyPrefix = "eai_";
    private const int RandomByteCount = 32;

    public GeneratedDeveloperApiKey Generate()
    {
        var plaintext = KeyPrefix + Base64Url(RandomNumberGenerator.GetBytes(RandomByteCount));
        var prefix = plaintext[..Math.Min(DeveloperApiKey.PrefixLength, plaintext.Length)];
        return new GeneratedDeveloperApiKey(plaintext, prefix, Hash(plaintext));
    }

    public string Hash(string plaintextKey)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey))
            return string.Empty;
        return Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(plaintextKey)));
    }

    public bool Verify(string plaintextKey, string expectedHash)
    {
        if (string.IsNullOrWhiteSpace(plaintextKey) || string.IsNullOrWhiteSpace(expectedHash))
            return false;

        byte[] actual;
        byte[] expected;
        try
        {
            actual = Convert.FromHexString(Hash(plaintextKey));
            expected = Convert.FromHexString(expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }

        return actual.Length == expected.Length && CryptographicOperations.FixedTimeEquals(actual, expected);
    }

    private static string Base64Url(byte[] bytes) =>
        Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
}
