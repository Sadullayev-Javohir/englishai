using Domain.Analytics;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Analytics;

/// <summary>
/// EF Core mapping for the product-event log that powers the founder activation funnel.
/// </summary>
public sealed class ProductEventConfiguration : IEntityTypeConfiguration<ProductEvent>
{
    public void Configure(EntityTypeBuilder<ProductEvent> builder)
    {
        builder.ToTable("ProductEvents");
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id).ValueGeneratedNever();

        builder.Property(e => e.LearnerId).IsRequired();
        builder.Property(e => e.Type).IsRequired();
        builder.Property(e => e.OccurredAt).IsRequired();
        builder.Property(e => e.Source).HasMaxLength(ProductEvent.MaxSourceLength);

        builder.HasIndex(e => e.OccurredAt);
        builder.HasIndex(e => new { e.LearnerId, e.Type });
        builder.HasIndex(e => new { e.LearnerId, e.Type, e.Source });
    }
}
