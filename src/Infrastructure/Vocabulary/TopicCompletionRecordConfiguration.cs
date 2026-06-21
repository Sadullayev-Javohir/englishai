using Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core mapping for <see cref="TopicCompletionRecord"/> (PROJECT-SPEC K.5). A unique (learner,
/// topic) index keeps it one row per pair; the per-module scores are an owned collection in a side
/// table so the aggregate is loaded and saved as a unit.
/// </summary>
public sealed class TopicCompletionRecordConfiguration : IEntityTypeConfiguration<TopicCompletionRecord>
{
    public void Configure(EntityTypeBuilder<TopicCompletionRecord> builder)
    {
        builder.ToTable("TopicCompletionRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.LearnerId).IsRequired();
        builder.Property(r => r.VocabularyTopicId).IsRequired();
        builder.HasIndex(r => new { r.LearnerId, r.VocabularyTopicId }).IsUnique();
        builder.HasIndex(r => new { r.LearnerId, r.MasteredAt });

        builder.Property(r => r.Level).HasConversion<string>().HasMaxLength(8);
        builder.Property(r => r.MasteredAt);
        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.UpdatedAt);

        builder.OwnsMany(r => r.ModuleScores, scores =>
        {
            scores.ToTable("TopicModuleScores");
            scores.WithOwner().HasForeignKey("TopicCompletionRecordId");
            scores.HasKey("TopicCompletionRecordId", nameof(TopicModuleScore.Module));

            scores.Property(s => s.Module).HasConversion<string>().HasMaxLength(16);
            scores.Property(s => s.Score);
            scores.Property(s => s.AchievedAt);
        });

        builder.Metadata
            .FindNavigation(nameof(TopicCompletionRecord.ModuleScores))!
            .SetPropertyAccessMode(PropertyAccessMode.Field);
    }
}
