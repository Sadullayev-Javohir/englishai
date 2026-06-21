using Domain.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Subscription;

/// <summary>EF Core mapping for the <see cref="Subscription"/> aggregate (one per learner).</summary>
public sealed class SubscriptionConfiguration : IEntityTypeConfiguration<Domain.Subscription.Subscription>
{
    public void Configure(EntityTypeBuilder<Domain.Subscription.Subscription> builder)
    {
        builder.ToTable("Subscriptions");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.LearnerId).IsRequired();
        builder.HasIndex(s => s.LearnerId).IsUnique();

        builder.Property(s => s.Status).HasConversion<int>();
        builder.Property(s => s.Plan).HasConversion<int?>();
        builder.Property(s => s.ExpiresAt);
        builder.Property(s => s.CreatedAt);
        builder.Property(s => s.UpdatedAt);

        // The daily expiry job scans by expiry; index it.
        builder.HasIndex(s => s.ExpiresAt);
    }
}
