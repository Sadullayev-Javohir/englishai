using Domain.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Subscription;

/// <summary>EF Core mapping for <see cref="PremiumWaitlistEntry"/>.</summary>
public sealed class PremiumWaitlistEntryConfiguration : IEntityTypeConfiguration<PremiumWaitlistEntry>
{
    public void Configure(EntityTypeBuilder<PremiumWaitlistEntry> builder)
    {
        builder.ToTable("PremiumWaitlistEntries");
        builder.HasKey(entry => entry.Id);
        builder.Property(entry => entry.Id).ValueGeneratedNever();

        builder.Property(entry => entry.Contact).IsRequired().HasMaxLength(128);
        // Unique on the NORMALIZED contact so a repeat submission is idempotent and nobody ends up
        // on the launch email three times.
        builder.HasIndex(entry => entry.Contact).IsUnique();

        builder.Property(entry => entry.ContactKind).HasConversion<int>();
        builder.Property(entry => entry.InterestedPlan).HasConversion<int?>();
        builder.Property(entry => entry.LearnerId);
        builder.Property(entry => entry.Source).HasMaxLength(64);
        builder.Property(entry => entry.CreatedAt);

        // The admin view lists newest first.
        builder.HasIndex(entry => entry.CreatedAt);
    }
}
