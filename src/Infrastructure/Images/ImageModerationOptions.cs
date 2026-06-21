namespace Infrastructure.Images;

public sealed class ImageModerationOptions
{
    public const string SectionName = "ImageModeration";

    public bool Enabled { get; set; }
    public bool RequireSafetyApproval { get; set; }
    public string BaseUrl { get; set; } = "http://image-moderation:8080";
    public int TimeoutSeconds { get; set; } = 20;
    public int CandidateMultiplier { get; set; } = 4;
}
