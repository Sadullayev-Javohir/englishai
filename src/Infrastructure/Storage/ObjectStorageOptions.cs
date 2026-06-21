namespace Infrastructure.Storage;

public sealed class ObjectStorageOptions
{
    public const string SectionName = "ObjectStorage";

    public bool Enabled { get; set; }
    public string LocalPath { get; set; } = Path.Combine(Path.GetTempPath(), "englishai-object-storage");
    public string ServiceUrl { get; set; } = string.Empty;
    public string Region { get; set; } = "fsn1";
    public string AccessKey { get; set; } = string.Empty;
    public string SecretKey { get; set; } = string.Empty;
    public string PublicBucket { get; set; } = string.Empty;
    public string PrivateBucket { get; set; } = string.Empty;
    public string PublicBaseUrl { get; set; } = string.Empty;
    public string KeyPrefix { get; set; } = "englishai";
    public int PrivateReadUrlMinutes { get; set; } = 5;

    public void Validate(bool production)
    {
        if (!Enabled)
            return;

        var missing = new List<string>();
        if (!Uri.TryCreate(ServiceUrl, UriKind.Absolute, out _))
            missing.Add(nameof(ServiceUrl));
        if (!Uri.TryCreate(PublicBaseUrl, UriKind.Absolute, out _))
            missing.Add(nameof(PublicBaseUrl));
        if (string.IsNullOrWhiteSpace(AccessKey))
            missing.Add(nameof(AccessKey));
        if (string.IsNullOrWhiteSpace(SecretKey))
            missing.Add(nameof(SecretKey));
        if (string.IsNullOrWhiteSpace(PublicBucket))
            missing.Add(nameof(PublicBucket));
        if (string.IsNullOrWhiteSpace(PrivateBucket))
            missing.Add(nameof(PrivateBucket));

        if (missing.Count > 0)
            throw new InvalidOperationException($"ObjectStorage configuration is incomplete: {string.Join(", ", missing)}.");
    }
}
