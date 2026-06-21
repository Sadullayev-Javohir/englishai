using Domain.Subscription;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Subscription;

/// <summary>EF Core mapping for <see cref="Payment"/> records, keyed for idempotent confirmation.</summary>
public sealed class PaymentConfiguration : IEntityTypeConfiguration<Payment>
{
    public void Configure(EntityTypeBuilder<Payment> builder)
    {
        builder.ToTable("Payments");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.LearnerId).IsRequired();
        builder.HasIndex(p => p.LearnerId);

        builder.Property(p => p.Plan).HasConversion<int>();
        builder.Property(p => p.AmountUzs);
        builder.Property(p => p.Provider).HasConversion<int>();

        builder.Property(p => p.TransactionId).IsRequired().HasMaxLength(128);
        builder.HasIndex(p => p.TransactionId).IsUnique();

        builder.Property(p => p.Status).HasConversion<int>();
        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.CompletedAt);
        builder.Property(p => p.ProviderTransactionId);
        builder.HasIndex(p => p.ProviderTransactionId).IsUnique();
        builder.Property(p => p.ProviderPaymentId);
        builder.Property(p => p.ProviderPrepareId);
        builder.HasIndex(p => p.ProviderPrepareId).IsUnique();
    }
}
