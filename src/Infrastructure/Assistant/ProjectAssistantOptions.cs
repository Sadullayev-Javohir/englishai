namespace Infrastructure.Assistant;

public sealed class ProjectAssistantOptions
{
    public const string SectionName = "ProjectAssistant";

    public int RequestTimeoutSeconds { get; set; } = 90;
}
