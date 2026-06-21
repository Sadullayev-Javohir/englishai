using System.Text.Json;
using Domain.Reading;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Reading;

/// <summary>
/// EF Core mapping for the <see cref="ReadingPassage"/> aggregate. The glossary and
/// comprehension questions are owned collections so they persist as one unit with the
/// passage; a question's options are stored as a JSON column. The CEFR level is indexed for
/// the adaptive catalog query.
/// </summary>
public sealed class ReadingPassageConfiguration : IEntityTypeConfiguration<ReadingPassage>
{
    public void Configure(EntityTypeBuilder<ReadingPassage> builder)
    {
        builder.ToTable("ReadingPassages");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.VocabularyTopicId);
        builder.HasIndex(p => p.VocabularyTopicId);
        builder.Property(p => p.Title).IsRequired().HasMaxLength(200);
        builder.Property(p => p.Body).IsRequired().HasColumnType("text");
        builder.Property(p => p.Topic).IsRequired().HasMaxLength(60);
        builder.Property(p => p.Category).IsRequired().HasMaxLength(80);
        builder.Property(p => p.ContextGaps).HasMaxLength(8000);
        builder.Property(p => p.Level).HasConversion<int>();
        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.CreatedAt);
        builder.HasIndex(p => p.Level);

        // WordCount is computed from Body; not persisted.
        builder.Ignore(p => p.WordCount);

        builder.OwnsMany(p => p.Glossary, b =>
        {
            b.ToTable("ReadingGlossaryEntries");
            b.WithOwner().HasForeignKey("ReadingPassageId");
            b.HasKey(g => g.Id);
            b.Property(g => g.Id).ValueGeneratedNever();
            b.Property(g => g.Word).IsRequired().HasMaxLength(120);
            b.Property(g => g.Translation).IsRequired().HasMaxLength(200);
            b.Property(g => g.ExampleSentence).HasMaxLength(2000);
        });

        builder.OwnsMany(p => p.Questions, b =>
        {
            b.ToTable("ReadingComprehensionQuestions");
            b.WithOwner().HasForeignKey("ReadingPassageId");
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

        builder.Navigation(p => p.Glossary).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
