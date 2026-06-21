using System.Text.Json;
using Domain.Video;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Video;

/// <summary>
/// EF Core mapping for the <see cref="VideoLesson"/> aggregate. The transcript and
/// comprehension questions are owned collections so they persist as one unit with the
/// lesson; a question's options are stored as a JSON column. The CEFR level is indexed
/// for the adaptive catalog query.
/// </summary>
public sealed class VideoLessonConfiguration : IEntityTypeConfiguration<VideoLesson>
{
    public void Configure(EntityTypeBuilder<VideoLesson> builder)
    {
        builder.ToTable("VideoLessons");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.YouTubeVideoId).IsRequired().HasMaxLength(20);
        builder.Property(v => v.Title).IsRequired().HasMaxLength(200);
        builder.Property(v => v.Channel).IsRequired().HasMaxLength(120);
        builder.Property(v => v.DurationSeconds);
        builder.Property(v => v.Topic).IsRequired().HasMaxLength(60);
        builder.Property(v => v.Level).HasConversion<int>();
        builder.Property(v => v.Status).HasConversion<int>();
        builder.Property(v => v.TranscriptStatus).HasConversion<int>();
        builder.Property(v => v.CreatedAt);
        builder.HasIndex(v => new { v.Status, v.Level, v.CreatedAt });
        builder.HasIndex(v => v.YouTubeVideoId).IsUnique();

        builder.OwnsMany(v => v.Transcript, b =>
        {
            b.ToTable("VideoTranscriptSegments");
            b.WithOwner().HasForeignKey("VideoLessonId");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.Property(s => s.StartSeconds);
            b.Property(s => s.EndSeconds);
            b.Property(s => s.EnglishText).IsRequired().HasMaxLength(1000);
            b.Property(s => s.UzbekTranslation).HasMaxLength(1000);

            // A segment's words carry per-word timing for the player's karaoke highlight. They are
            // owned by the segment and persist in their own table (EF Core does not allow a JSON
            // column inside an owned type that is itself mapped to a table).
            b.OwnsMany(s => s.Words, w =>
            {
                w.ToTable("VideoTranscriptWords");
                w.WithOwner().HasForeignKey("TranscriptSegmentId");
                w.Property(x => x.Text).IsRequired().HasMaxLength(60);
                w.Property(x => x.StartSeconds);
                w.Property(x => x.EndSeconds);
            });
            b.Navigation(s => s.Words).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.OwnsMany(v => v.Questions, b =>
        {
            b.ToTable("VideoComprehensionQuestions");
            b.WithOwner().HasForeignKey("VideoLessonId");
            b.HasKey(q => q.Id);
            b.Property(q => q.Id).ValueGeneratedNever();
            b.Property(q => q.Prompt).IsRequired().HasMaxLength(500);
            b.Property(q => q.CorrectOptionIndex);
            b.Property(q => q.HintCode).HasMaxLength(80);

            b.Property(q => q.Options)
                .HasConversion(
                    options => JsonSerializer.Serialize(options, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (a, c) => a!.SequenceEqual(c!),
                    v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                    v => v.ToList()));
        });

        // The word glossary is a small, read-as-a-unit list, so it persists as a single JSON
        // column on the lesson row rather than its own table.
        builder.OwnsMany(v => v.Glossary, b =>
        {
            b.ToJson();
            b.Property(g => g.Word).HasMaxLength(40);
            b.Property(g => g.UzbekMeaning).HasMaxLength(120);
        });

        builder.Navigation(v => v.Transcript).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(v => v.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(v => v.Glossary).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
