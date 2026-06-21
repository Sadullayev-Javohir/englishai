namespace Infrastructure.Llm;

public sealed class SolPrimaryOptions
{
    public const string SectionName = "SolPrimary";

    public bool Enabled { get; set; } = true;
    public string Endpoint { get; set; } = string.Empty;
    public string ApiKey { get; set; } = string.Empty;
    public string Model { get; set; } = "gpt-5.6-sol";

    // gpt-5.x SOL models are reasoning models: without this they spend the whole output-token
    // budget on hidden reasoning and return empty visible content, which the fallback layer reads
    // as "SOL unavailable" and silently routes every conversation turn to the slower Hermes model.
    // The conversational tutor only needs one short sentence, so default to no reasoning. Supported
    // values for this model: none, low, medium, high, xhigh. Blank -> omit the field (safe for
    // non-reasoning endpoints).
    public string? ReasoningEffort { get; set; } = "none";

    public int RequestTimeoutSeconds { get; set; } = 25;
    public int MaxConcurrency { get; set; } = 1;
    public int TokenLimitPerWindow { get; set; } = 1_000_000;
    public int TokenReservationPerRequest { get; set; } = 200_000;
    public int TokenSafetyReserve { get; set; } = 100_000;
    public int TokenWindowSeconds { get; set; } = 60;

    public bool IsConfigured => Enabled
                                && Uri.TryCreate(Endpoint, UriKind.Absolute, out _)
                                && !string.IsNullOrWhiteSpace(ApiKey)
                                && !string.IsNullOrWhiteSpace(Model);
}
