using Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core mapping for the <see cref="VocabularyItem"/> aggregate. The SRS
/// <see cref="ReviewSchedule"/> is an owned one-to-one stored inline, with the due
/// timestamp indexed so the daily notification job can scan due items efficiently.
/// </summary>
public sealed class VocabularyItemConfiguration : IEntityTypeConfiguration<VocabularyItem>
{
    public void Configure(EntityTypeBuilder<VocabularyItem> builder)
    {
        builder.ToTable("VocabularyItems");
        builder.HasKey(v => v.Id);
        builder.Property(v => v.Id).ValueGeneratedNever();

        builder.Property(v => v.LearnerId).IsRequired();
        builder.HasIndex(v => new { v.LearnerId, v.CreatedAt });

        builder.Property(v => v.Word).IsRequired().HasMaxLength(100);
        builder.Property(v => v.Translation).IsRequired().HasMaxLength(200);
        builder.Property(v => v.ExampleSentence).HasMaxLength(2000);
        builder.Property(v => v.Source).HasConversion<int>();
        builder.Property(v => v.SourceTopicId);
        builder.HasIndex(v => new { v.LearnerId, v.SourceTopicId, v.Word })
            .IsUnique()
            .HasFilter("\"SourceTopicId\" IS NOT NULL");
        builder.Property(v => v.PartOfSpeech).HasConversion<int>();
        builder.Property(v => v.CreatedAt);

        builder.OwnsOne(v => v.Schedule, b =>
        {
            b.Property(s => s.Id).ValueGeneratedNever();
            b.Property(s => s.LearnedAt);
            b.Property(s => s.NextReviewAt);
            b.Property(s => s.Stage).HasConversion<int>();
            b.Property(s => s.FailCount);
            b.HasIndex(s => s.NextReviewAt);
        });

        builder.Navigation(v => v.Schedule).IsRequired();
    }
}
