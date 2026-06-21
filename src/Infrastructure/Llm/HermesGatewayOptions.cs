namespace Infrastructure.Llm;

public sealed class HermesGatewayOptions
{
    public const string SectionName = "HermesGateway";

    public string? ApiKey { get; set; }
    public string Model { get; set; } = "hermes-agent";
    public string Endpoint { get; set; } = "http://127.0.0.1:8642/v1";
    public int MaxOutputTokens { get; set; } = 1400;
    public int RequestTimeoutSeconds { get; set; } = 30;
    public int MaxConcurrency { get; set; } = 32;
    public int MaxQueueLength { get; set; } = 2000;
    public int QueueTimeoutSeconds { get; set; } = 45;
    public int CircuitFailureThreshold { get; set; } = 5;
    public int CircuitBreakSeconds { get; set; } = 30;

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ResolvedApiKey);

    public string ResolvedApiKey => ResolvePrimaryKey();

    private string ResolvePrimaryKey()
    {
        if (!string.IsNullOrWhiteSpace(ApiKey))
            return ApiKey.Trim();

        var configured = Environment.GetEnvironmentVariable("HermesGateway__ApiKey");
        if (!string.IsNullOrWhiteSpace(configured))
            return configured.Trim();

        return Environment.GetEnvironmentVariable("HERMES_GATEWAY_API_KEY")?.Trim() ?? string.Empty;
    }
}
