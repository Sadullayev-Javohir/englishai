using System.Text.Json;
using Domain.Writing;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Writing;

/// <summary>
/// EF Core mapping for the <see cref="WritingTask"/> aggregate. Tasks are now generated lazily per
/// learning-spine topic and cached, so the vocabulary-topic link and lazy-fill status are persisted;
/// the guidance hints are stored as a JSON column. The CEFR level is indexed for catalog queries.
/// Assessments are not persisted here - they are computed on demand and recorded as Writing skill
/// activity on the learner profile and the topic-completion record.
/// </summary>
public sealed class WritingTaskConfiguration : IEntityTypeConfiguration<WritingTask>
{
    public void Configure(EntityTypeBuilder<WritingTask> builder)
    {
        builder.ToTable("WritingTasks");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.VocabularyTopicId);
        builder.HasIndex(t => t.VocabularyTopicId);
        builder.Property(t => t.Prompt).IsRequired().HasMaxLength(4000);
        builder.Property(t => t.Level).HasConversion<int>();
        builder.Property(t => t.MinWords);
        builder.Property(t => t.MaxWords);
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.CreatedAt);
        builder.HasIndex(t => t.Level);

        var guidance = builder.Property(t => t.Guidance)
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                json => string.IsNullOrWhiteSpace(json)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
            .UsePropertyAccessMode(PropertyAccessMode.Field);

        guidance.Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
            (a, c) => a!.SequenceEqual(c!),
            v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
            v => v.ToList()));
    }
}
