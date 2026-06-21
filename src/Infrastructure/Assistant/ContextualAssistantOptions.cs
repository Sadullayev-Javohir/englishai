namespace Infrastructure.Assistant;

public sealed class ContextualAssistantOptions
{
    public const string SectionName = "ContextualAssistant";

    public int RequestTimeoutSeconds { get; set; } = 40;
}
