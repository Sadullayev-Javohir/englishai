using Domain.Common;

namespace Domain.Developer;

/// <summary>
/// A developer credential owned by one EnglishAI account. Only the SHA-256 hash is persisted;
/// the plaintext token is shown exactly once when it is created.
/// </summary>
public sealed class DeveloperApiKey
{
    public const int MaxNameLength = 80;
    public const int PrefixLength = 12;

    private DeveloperApiKey()
    {
        Name = string.Empty;
        Prefix = string.Empty;
        KeyHash = string.Empty;
    }

    private DeveloperApiKey(
        Guid id,
        Guid userId,
        string name,
        string prefix,
        string keyHash,
        DateTimeOffset createdAt)
    {
        Id = id;
        UserId = userId;
        Name = name;
        Prefix = prefix;
        KeyHash = keyHash;
        CreatedAt = createdAt;
    }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string Name { get; private set; }
    public string Prefix { get; private set; }
    public string KeyHash { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset? LastUsedAt { get; private set; }
    public DateTimeOffset? RevokedAt { get; private set; }
    public bool IsActive => RevokedAt is null;

    public static DeveloperApiKey Create(
        Guid userId,
        string name,
        string prefix,
        string keyHash,
        DateTimeOffset now)
    {
        if (userId == Guid.Empty)
            throw new DomainException("API key owner must not be empty.");
        if (string.IsNullOrWhiteSpace(name) || name.Trim().Length > MaxNameLength)
            throw new DomainException($"API key name must be between 1 and {MaxNameLength} characters.");
        if (string.IsNullOrWhiteSpace(prefix) || prefix.Length > PrefixLength)
            throw new DomainException("API key prefix is invalid.");
        if (string.IsNullOrWhiteSpace(keyHash))
            throw new DomainException("API key hash must not be empty.");

        return new DeveloperApiKey(Guid.NewGuid(), userId, name.Trim(), prefix, keyHash, now);
    }

    public void RecordUsage(DateTimeOffset now)
    {
        if (!IsActive)
            throw new DomainException("A revoked API key cannot be used.");
        LastUsedAt = now;
    }

    public void Revoke(DateTimeOffset now)
    {
        if (RevokedAt is null)
            RevokedAt = now;
    }
}
