using Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Analytics;

/// <summary>
/// EF Core mapping for <see cref="DailyStudyRecord"/> - one row per (learner, day). A unique
/// (learner, day) index keeps upserts one-row and makes the dashboard's per-learner reads fast.
/// </summary>
public sealed class DailyStudyRecordConfiguration : IEntityTypeConfiguration<DailyStudyRecord>
{
    public void Configure(EntityTypeBuilder<DailyStudyRecord> builder)
    {
        builder.ToTable("DailyStudyRecords");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.LearnerId).IsRequired();
        builder.Property(r => r.Day).IsRequired();
        builder.HasIndex(r => new { r.LearnerId, r.Day }).IsUnique();

        builder.Property(r => r.SpeakingSeconds);
        builder.Property(r => r.ListeningSeconds);
        builder.Property(r => r.ReadingSeconds);
        builder.Property(r => r.WritingSeconds);
        builder.Property(r => r.GrammarSeconds);
        builder.Property(r => r.VocabularySeconds);

        // TotalSeconds is computed from the per-skill columns - never stored.
        builder.Ignore(r => r.TotalSeconds);
    }
}
