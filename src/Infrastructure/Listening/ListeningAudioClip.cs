using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Listening;

/// <summary>
/// Durable cache row for a synthesized listening clip: the Azure Neural TTS audio for one exercise's
/// transcript, keyed by exercise id. The clip is identical for every learner, so it is synthesized
/// once and reused across requests <em>and across process restarts</em> (docs/development-guide.md rule 10 - avoid
/// repeated Azure TTS cost). This is an infrastructure persistence row, not a domain aggregate.
/// </summary>
public sealed class ListeningAudioClip
{
    public Guid ExerciseId { get; set; }

    public byte[]? AudioContent { get; set; }
    public string ContentType { get; set; } = "audio/mpeg";
    public string? ObjectKey { get; set; }
    public long? SizeBytes { get; set; }
    public string? Checksum { get; set; }
    public string? ObjectETag { get; set; }
    public DateTimeOffset? StoredAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
}

/// <summary>EF Core mapping for <see cref="ListeningAudioClip"/> (a bytea-backed audio cache table).</summary>
public sealed class ListeningAudioClipConfiguration : IEntityTypeConfiguration<ListeningAudioClip>
{
    public void Configure(EntityTypeBuilder<ListeningAudioClip> builder)
    {
        builder.ToTable("ListeningAudioClips");
        builder.HasKey(c => c.ExerciseId);
        builder.Property(c => c.ExerciseId).ValueGeneratedNever();
        builder.Property(c => c.AudioContent);
        builder.Property(c => c.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(c => c.ObjectKey).HasMaxLength(512);
        builder.Property(c => c.Checksum).HasMaxLength(64);
        builder.Property(c => c.ObjectETag).HasMaxLength(160);
        builder.Property(c => c.CreatedAt);
    }
}
