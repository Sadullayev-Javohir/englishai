namespace Infrastructure.Identity.Avatar;

public sealed class UserAvatar
{
    public Guid UserId { get; set; }
    public byte[]? Data { get; set; }
    public string ContentType { get; set; } = "image/webp";
    public string? ObjectKey { get; set; }
    public long? SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public string? ObjectETag { get; set; }
    public DateTimeOffset? StoredAt { get; set; }
    public DateTimeOffset UpdatedAt { get; set; }
}
