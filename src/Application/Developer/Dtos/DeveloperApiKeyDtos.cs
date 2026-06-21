using Domain.Developer;

namespace Application.Developer.Dtos;

public sealed record DeveloperApiKeyDto(
    Guid Id,
    string Name,
    string Prefix,
    DateTimeOffset CreatedAt,
    DateTimeOffset? LastUsedAt,
    DateTimeOffset? RevokedAt,
    bool IsActive)
{
    public static DeveloperApiKeyDto From(DeveloperApiKey apiKey) =>
        new(
            apiKey.Id,
            apiKey.Name,
            apiKey.Prefix,
            apiKey.CreatedAt,
            apiKey.LastUsedAt,
            apiKey.RevokedAt,
            apiKey.IsActive);
}

public sealed record CreatedDeveloperApiKeyDto(
    Guid Id,
    string Name,
    string Prefix,
    string ApiKey,
    DateTimeOffset CreatedAt);
