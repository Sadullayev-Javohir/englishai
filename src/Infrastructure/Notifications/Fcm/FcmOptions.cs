namespace Infrastructure.Notifications.Fcm;

/// <summary>
/// Firebase Cloud Messaging configuration (bound from the <c>Fcm</c> section / secrets). Supply the
/// Firebase project id and the service-account credentials - either inline JSON
/// (<see cref="ServiceAccountJson"/>) or a path to the downloaded key file
/// (<see cref="ServiceAccountJsonPath"/>). When neither is set, push is not configured and a no-op
/// notifier is wired (the app runs without Firebase, mirroring the other secret-gated adapters).
/// </summary>
public sealed class FcmOptions
{
    public const string SectionName = "Fcm";

    /// <summary>The Firebase project id (also present inside the service-account JSON as <c>project_id</c>).</summary>
    public string? ProjectId { get; set; }

    /// <summary>The full service-account JSON, inlined (e.g. from an environment variable / secret).</summary>
    public string? ServiceAccountJson { get; set; }

    /// <summary>Path to the service-account JSON key file on disk (alternative to inlining it).</summary>
    public string? ServiceAccountJsonPath { get; set; }

    /// <summary>Resolves the service-account JSON from the inline value or the file path, if any.</summary>
    public string? ResolveServiceAccountJson()
    {
        if (!string.IsNullOrWhiteSpace(ServiceAccountJson))
            return ServiceAccountJson;
        if (!string.IsNullOrWhiteSpace(ServiceAccountJsonPath) && File.Exists(ServiceAccountJsonPath))
            return File.ReadAllText(ServiceAccountJsonPath);
        return null;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(ResolveServiceAccountJson());
}
