namespace Application.Developer.Ports;

public interface IDeveloperApiKeyProtector
{
    GeneratedDeveloperApiKey Generate();
    string Hash(string plaintextKey);
    bool Verify(string plaintextKey, string expectedHash);
}

public sealed record GeneratedDeveloperApiKey(string Plaintext, string Prefix, string Hash);
