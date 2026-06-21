namespace Infrastructure.Developer;

public sealed class DeveloperAiOptions
{
    public const string SectionName = "DeveloperAi";

    public string BaseUrl { get; set; } = "http://127.0.0.1:8642/v1";
    public string Model { get; set; } = "hermes-agent";
    public string[] ApiKeys { get; set; } = Array.Empty<string>();
    public int MaxOutputTokens { get; set; } = 2048;
    public long MaxRequestBytes { get; set; } = 65_536;
    public int MaxConcurrentRequests { get; set; } = 4;
    public int RequestTimeoutSeconds { get; set; } = 120;

    public IReadOnlyList<string> ResolvedApiKeys => ApiKeys
        .Select(k => k?.Trim())
        .Where(k => !string.IsNullOrWhiteSpace(k))
        .Select(k => k!)
        .Distinct(StringComparer.Ordinal)
        .ToArray();
}
