namespace Infrastructure.Jobs;

public static class HangfireQueues
{
    public const string Critical = "critical";
    public const string Notifications = "notifications";
    public const string Analytics = "analytics";
    public const string Content = "content";
    public const string Maintenance = "maintenance";

    public static readonly IReadOnlySet<string> All = new HashSet<string>(StringComparer.Ordinal)
    {
        Critical,
        Notifications,
        Analytics,
        Content,
        Maintenance,
    };
}
