using Domain.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Gamification;

/// <summary>
/// EF Core mapping for <see cref="LearnerPoints"/> - one durable ledger row per learner
/// (leaderboard/points feature). A unique index on <c>LearnerId</c> keeps upserts one-row.
/// </summary>
public sealed class LearnerPointsConfiguration : IEntityTypeConfiguration<LearnerPoints>
{
    public void Configure(EntityTypeBuilder<LearnerPoints> builder)
    {
        builder.ToTable("LearnerPoints");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.LearnerId).IsRequired();
        builder.HasIndex(p => p.LearnerId).IsUnique();

        builder.Property(p => p.LifetimeXp).IsRequired();
        builder.Property(p => p.SpendableCoins).IsRequired();
        builder.Property(p => p.HighestStreakMilestoneReached).IsRequired();
        builder.Property(p => p.Energy).IsRequired().HasDefaultValue(EnergyPolicy.MaximumEnergy);
        // The regeneration anchor is a plain timestamp now that energy comes back one unit every
        // 36 minutes - there is no hour boundary to align a new row to.
        builder.Property(p => p.EnergyRefilledAt).IsRequired().HasDefaultValueSql("now() at time zone 'utc'");
        builder.Property(p => p.CreatedAt).IsRequired();
        builder.Property(p => p.UpdatedAt).IsRequired();
    }
}
