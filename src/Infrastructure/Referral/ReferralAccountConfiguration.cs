using Domain.Referral;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Referral;

/// <summary>EF Core mapping for the <see cref="ReferralAccount"/> aggregate (one per learner).</summary>
public sealed class ReferralAccountConfiguration : IEntityTypeConfiguration<ReferralAccount>
{
    public void Configure(EntityTypeBuilder<ReferralAccount> builder)
    {
        builder.ToTable("ReferralAccounts");
        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).ValueGeneratedNever();

        builder.Property(a => a.LearnerId).IsRequired();
        builder.HasIndex(a => a.LearnerId).IsUnique();

        builder.Property(a => a.Code).IsRequired().HasMaxLength(ReferralCode.Length);
        builder.HasIndex(a => a.Code).IsUnique();

        builder.Property(a => a.RewardedCount);
        builder.Property(a => a.BonusTopicAllowance);
        builder.Property(a => a.RemainingSpeakingCredits);
        builder.Property(a => a.RemainingWritingCredits);
        builder.Property(a => a.CreatedAt);
        builder.Property(a => a.UpdatedAt);
    }
}
