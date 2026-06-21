using Domain.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Gamification;

/// <summary>
/// EF Core mapping for <see cref="DiscountRedemption"/> coupons (leaderboard/points feature).
/// <c>Code</c> is unique (it's the checkout lookup key); <c>LearnerId</c> is indexed for the
/// "my active coupons" read.
/// </summary>
public sealed class DiscountRedemptionConfiguration : IEntityTypeConfiguration<DiscountRedemption>
{
    public void Configure(EntityTypeBuilder<DiscountRedemption> builder)
    {
        builder.ToTable("DiscountRedemptions");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.LearnerId).IsRequired();
        builder.HasIndex(r => r.LearnerId);

        builder.Property(r => r.Code).IsRequired().HasMaxLength(16);
        builder.HasIndex(r => r.Code).IsUnique();

        builder.Property(r => r.CoinsSpent).IsRequired();
        builder.Property(r => r.DiscountPercent).IsRequired();
        builder.Property(r => r.Status).IsRequired();
        builder.Property(r => r.CreatedAt).IsRequired();
        builder.Property(r => r.ExpiresAt).IsRequired();
        builder.Property(r => r.UsedAt);
    }
}
