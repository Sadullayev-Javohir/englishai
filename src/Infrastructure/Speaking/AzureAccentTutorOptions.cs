namespace Infrastructure.Speaking;

public sealed class AzureAccentTutorOptions
{
    public const string SectionName = "AzureAccentTutors";

    public string Endpoint { get; set; } =
        "https://javohirsadullayev2024-2-resource.services.ai.azure.com/api/projects/javohirsadullayev2024-2939";

    public bool Enabled { get; set; }

    // Optional Entra ID service-principal credentials. When all three are present the agent
    // authenticates with a ClientSecretCredential (works in dev via user-secrets and in prod via
    // env/.env). When absent it falls back to DefaultAzureCredential (managed identity / az login).
    public string TenantId { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public Dictionary<string, AzureAccentTutorDefinition> Tutors { get; set; } = DefaultTutors();

    public bool IsConfigured => Enabled && Uri.TryCreate(Endpoint, UriKind.Absolute, out _);

    public bool HasServicePrincipal =>
        !string.IsNullOrWhiteSpace(TenantId) &&
        !string.IsNullOrWhiteSpace(ClientId) &&
        !string.IsNullOrWhiteSpace(ClientSecret);

    private static Dictionary<string, AzureAccentTutorDefinition> DefaultTutors() =>
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["american"] = new("2", "2", "en-US-AvaMultilingualNeural"),
            ["british"] = new("3", "3", "en-GB-SoniaNeural"),
            ["australian"] = new("5", "2", "en-AU-NatashaNeural"),
            ["irish"] = new("8", "2", "en-IE-EmilyNeural"),
        };
}

public sealed record AzureAccentTutorDefinition(string AgentName, string AgentVersion, string VoiceName);
