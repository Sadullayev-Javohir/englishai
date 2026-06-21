using Domain.Learning;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Learning;

/// <summary>
/// EF Core mapping for the <see cref="LearnerProfile"/> aggregate. The seed, activity
/// and error collections are modelled as owned entities so they load and save as one
/// unit with the profile (true aggregate persistence). Enums map to their int values.
/// </summary>
public sealed class LearnerProfileConfiguration : IEntityTypeConfiguration<LearnerProfile>
{
    public void Configure(EntityTypeBuilder<LearnerProfile> builder)
    {
        builder.ToTable("LearnerProfiles");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.LearnerId).IsRequired();
        builder.HasIndex(p => p.LearnerId).IsUnique();

        builder.Property(p => p.OverallLevel).HasConversion<int>();
        builder.Property(p => p.ConfirmationTestPassedAt);
        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);

        // Faza 7 retention: engagement timestamp + win-back dedup stage (PROJECT-SPEC I.1/I.2).
        builder.Property(p => p.LastActivityAt);
        builder.Property(p => p.LastWinBackStage).HasConversion<int>();

        // Onboarding learning goal (goal-based onboarding). Stored as its int value; defaults to
        // Unspecified (0) so existing profiles backfill without a data migration.
        builder.Property(p => p.LearningGoal)
            .HasConversion<int>()
            .HasDefaultValue(Domain.Common.LearningGoal.Unspecified);

        builder.OwnsMany(p => p.Seeds, b =>
        {
            b.ToTable("LearnerSkillSeeds");
            b.WithOwner().HasForeignKey("LearnerProfileId");
            b.HasKey(s => s.Id);
            b.Property(s => s.Id).ValueGeneratedNever();
            b.Property(s => s.Skill).HasConversion<int>();
            b.Property(s => s.Score);
        });

        builder.OwnsMany(p => p.Activities, b =>
        {
            b.ToTable("LearnerSkillActivities");
            b.WithOwner().HasForeignKey("LearnerProfileId");
            b.HasKey(a => a.Id);
            b.Property(a => a.Id).ValueGeneratedNever();
            b.Property(a => a.Skill).HasConversion<int>();
            b.Property(a => a.Score);
            b.Property(a => a.OccurredAt);
            b.HasIndex(a => a.OccurredAt);
        });

        builder.OwnsMany(p => p.Errors, b =>
        {
            b.ToTable("LearnerErrorObservations");
            b.WithOwner().HasForeignKey("LearnerProfileId");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.Category).HasConversion<int>();
            b.Property(e => e.Skill).HasConversion<int>();
            b.Property(e => e.OccurredAt);
            b.Property(e => e.Source).HasMaxLength(64);
            b.Property(e => e.SourceId);
            b.Property(e => e.Prompt).HasMaxLength(500);
            b.Property(e => e.LearnerAnswer).HasMaxLength(500);
            b.Property(e => e.ExpectedAnswer).HasMaxLength(500);
            b.Property(e => e.Explanation).HasMaxLength(1000);
            b.HasIndex(e => e.OccurredAt);
            b.HasIndex(e => new { e.Source, e.SourceId });
        });

        // Owned collections are read through get-only properties backed by private fields.
        builder.Navigation(p => p.Seeds).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Activities).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(p => p.Errors).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
