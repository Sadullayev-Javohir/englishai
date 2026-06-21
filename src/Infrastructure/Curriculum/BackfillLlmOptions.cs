namespace Infrastructure.Curriculum;

public sealed class BackfillLlmOptions
{
    public const string SectionName = "BackfillLlm";
    public string BaseUrl { get; set; } = "https://ai.edubase.uz/v1";
    public string Model { get; set; } = "gpt-5.6-sol";
    public string? ApiKey { get; set; }
    public int RequestTimeoutSeconds { get; set; } = 180;
    public int MaxOutputTokens { get; set; } = 8192;
    public int MaxAttempts { get; set; } = 4;
    public string ResolvedApiKey => !string.IsNullOrWhiteSpace(ApiKey)
        ? ApiKey.Trim()
        : Environment.GetEnvironmentVariable("BACKFILL_OPENAI_API_KEY")?.Trim()
          ?? Environment.GetEnvironmentVariable("OPENAI_API_KEY")?.Trim()
          ?? string.Empty;
    public string ResolvedBaseUrl => Environment.GetEnvironmentVariable("BACKFILL_OPENAI_BASE_URL")?.Trim()
        ?? Environment.GetEnvironmentVariable("OPENAI_BASE_URL")?.Trim()
        ?? BaseUrl.Trim();
    public string ResolvedModel => Environment.GetEnvironmentVariable("BACKFILL_OPENAI_MODEL")?.Trim()
        ?? Model.Trim();
}
