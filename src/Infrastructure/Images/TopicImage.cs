using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Images;

/// <summary>
/// Durable cache row for one image in a topic's gallery: a licensed photo downloaded once for one
/// learning-spine topic and stored in the database (docs/development-guide.md rule 12 - fetch once, store, reuse;
/// never hot-link a copyrighted source). A topic owns several images, each at its own
/// <see cref="Slot"/> (0 = cover/thumbnail, 1..N illustrate the topic in different places). Images
/// are identical for every learner and shared by every skill that teaches the topic, so they are
/// downloaded once and re-served from here across requests and process restarts (rule 10). The
/// bytes live in a <c>bytea</c> column, mirroring the
/// <see cref="Infrastructure.Listening.ListeningAudioClip"/> audio cache. This is an infrastructure
/// persistence row, not a domain aggregate.
/// </summary>
public sealed class TopicImage
{
    /// <summary>The learning-spine topic id this image illustrates (part of the primary key).</summary>
    public Guid TopicId { get; set; }

    /// <summary>The gallery slot within the topic (0 = cover); together with TopicId the primary key.</summary>
    public int Slot { get; set; }

    /// <summary>The downloaded image bytes (JPEG/PNG/…).</summary>
    public byte[]? Data { get; set; }

    /// <summary>The MIME type to serve the bytes with (e.g. "image/jpeg").</summary>
    public string ContentType { get; set; } = "image/jpeg";
    public string? ObjectKey { get; set; }
    public long? SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public string? ObjectETag { get; set; }
    public DateTimeOffset? StoredAt { get; set; }

    /// <summary>The provider the image came from (e.g. "Unsplash") - kept for provenance.</summary>
    public string Source { get; set; } = string.Empty;

    /// <summary>Human-readable attribution string to display (rule 12), or null.</summary>
    public string? Attribution { get; set; }

    /// <summary>The original source page URL (for attribution / re-download), or null.</summary>
    public string? SourceUrl { get; set; }

    public int Width { get; set; }
    public int Height { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public Application.Common.ImageSafetyStatus SafetyStatus { get; set; }
    public string? SafetyModelVersion { get; set; }
    public DateTimeOffset? SafetyCheckedAt { get; set; }
    public string? SafetyReasons { get; set; }
}

/// <summary>EF Core mapping for <see cref="TopicImage"/> (a bytea-backed image cache table).</summary>
public sealed class TopicImageConfiguration : IEntityTypeConfiguration<TopicImage>
{
    public void Configure(EntityTypeBuilder<TopicImage> builder)
    {
        builder.ToTable("TopicImages");
        builder.HasKey(i => new { i.TopicId, i.Slot });
        builder.Property(i => i.TopicId).ValueGeneratedNever();
        builder.Property(i => i.Slot).ValueGeneratedNever();
        builder.Property(i => i.Data);
        builder.Property(i => i.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(i => i.ObjectKey).HasMaxLength(512);
        builder.Property(i => i.Checksum).HasMaxLength(64);
        builder.Property(i => i.ObjectETag).HasMaxLength(160);
        builder.Property(i => i.Source).IsRequired().HasMaxLength(60);
        builder.Property(i => i.Attribution).HasMaxLength(300);
        builder.Property(i => i.SourceUrl).HasMaxLength(500);
        builder.Property(i => i.Width);
        builder.Property(i => i.Height);
        builder.Property(i => i.CreatedAt);
        builder.Property(i => i.SafetyStatus);
        builder.Property(i => i.SafetyModelVersion).HasMaxLength(120);
        builder.Property(i => i.SafetyCheckedAt);
        builder.Property(i => i.SafetyReasons).HasMaxLength(500);
    }
}
