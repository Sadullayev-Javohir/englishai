using System.Text.Json;
using Domain.Listening;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Listening;

/// <summary>
/// EF Core mapping for the <see cref="ListeningExercise"/> aggregate. The comprehension
/// questions and audio segments are owned collections so they persist as one unit; a
/// question's options are stored as a JSON column. The CEFR level is indexed for the adaptive
/// catalog query.
/// </summary>
public sealed class ListeningExerciseConfiguration : IEntityTypeConfiguration<ListeningExercise>
{
    public void Configure(EntityTypeBuilder<ListeningExercise> builder)
    {
        builder.ToTable("ListeningExercises");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.VocabularyTopicId);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        // Empty until a topic-scoped exercise is filled, so it is not required at the column level.
        builder.Property(e => e.Transcript).HasColumnType("text");
        builder.Property(e => e.Topic).IsRequired().HasMaxLength(60);
        builder.Property(e => e.Level).HasConversion<int>();
        builder.Property(e => e.Status).HasConversion<int>();
        builder.Property(e => e.CreatedAt);
        builder.HasIndex(e => e.Level);
        // The catalog/exercise lookups query by topic; index it.
        builder.HasIndex(e => e.VocabularyTopicId);

        // WordCount is computed from Transcript; not persisted.
        builder.Ignore(e => e.WordCount);

        builder.OwnsMany(e => e.Questions, b =>
        {
            b.ToTable("ListeningComprehensionQuestions");
            b.WithOwner().HasForeignKey("ListeningExerciseId");
            b.HasKey(q => q.Id);
            b.Property(q => q.Id).ValueGeneratedNever();
            b.Property(q => q.Prompt).IsRequired().HasMaxLength(2000);
            b.Property(q => q.CorrectOptionIndex);
            b.Property(q => q.HintCode).HasMaxLength(80);
            b.Property(q => q.Explanation).HasMaxLength(2000);

            b.Property(q => q.Options)
                .HasConversion(
                    options => JsonSerializer.Serialize(options, (JsonSerializerOptions?)null),
                    json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (a, c) => a!.SequenceEqual(c!),
                    v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                    v => v.ToList()));
        });

        builder.OwnsMany(e => e.Segments, b =>
        {
            b.ToTable("ListeningSegments");
            b.WithOwner().HasForeignKey("ListeningExerciseId");
            b.HasKey(segment => segment.Id);
            b.Property(segment => segment.Id).ValueGeneratedNever();
            b.Property(segment => segment.Order);
            b.Property(segment => segment.StartMs);
            b.Property(segment => segment.EndMs);
            b.Property(segment => segment.Speaker).IsRequired().HasMaxLength(100);
            b.Property(segment => segment.Text).IsRequired().HasMaxLength(2000);
            b.HasIndex("ListeningExerciseId", nameof(ListeningSegment.Order));
        });

        builder.Navigation(e => e.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(e => e.Segments).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
