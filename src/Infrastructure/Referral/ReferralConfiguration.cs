using Domain.Referral;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Referral;

/// <summary>EF Core mapping for the <see cref="Referral"/> aggregate (one per referred learner).</summary>
public sealed class ReferralConfiguration : IEntityTypeConfiguration<Domain.Referral.Referral>
{
    public void Configure(EntityTypeBuilder<Domain.Referral.Referral> builder)
    {
        builder.ToTable("Referrals");
        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).ValueGeneratedNever();

        builder.Property(r => r.ReferrerId).IsRequired();
        builder.HasIndex(r => r.ReferrerId);

        builder.Property(r => r.RefereeId).IsRequired();
        // At most one referral per referee - the primary anti-abuse guard.
        builder.HasIndex(r => r.RefereeId).IsUnique();

        builder.Property(r => r.Code).IsRequired().HasMaxLength(ReferralCode.Length);
        builder.Property(r => r.Status).HasConversion<int>();
        builder.Property(r => r.CreatedAt);
        builder.Property(r => r.QualifiedAt);
    }
}
