using Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core mapping for <see cref="TopicSpeakingProgress"/> - a learner's speaking practice toward
/// learning each vocabulary topic. A unique (learner, topic) index keeps it one row per pair so
/// reads and upserts stay simple.
/// </summary>
public sealed class TopicSpeakingProgressConfiguration : IEntityTypeConfiguration<TopicSpeakingProgress>
{
    public void Configure(EntityTypeBuilder<TopicSpeakingProgress> builder)
    {
        builder.ToTable("TopicSpeakingProgress");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.LearnerId).IsRequired();
        builder.Property(p => p.VocabularyTopicId).IsRequired();
        builder.HasIndex(p => new { p.LearnerId, p.VocabularyTopicId }).IsUnique();
        builder.HasIndex(p => new { p.LearnerId, p.LearnedAt });

        builder.Property(p => p.SpokenSeconds);
        builder.Property(p => p.LearnedAt);
        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);
    }
}
