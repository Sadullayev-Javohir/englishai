using Domain.Retention;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Retention;

/// <summary>
/// EF Core mapping for the <see cref="FeatureFlag"/> aggregate (PROJECT-SPEC I.3). Variants
/// are an owned collection so they persist as one unit with the flag. The key is unique and
/// indexed for fast lookup.
/// </summary>
public sealed class FeatureFlagConfiguration : IEntityTypeConfiguration<FeatureFlag>
{
    public void Configure(EntityTypeBuilder<FeatureFlag> builder)
    {
        builder.ToTable("FeatureFlags");
        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).ValueGeneratedNever();

        builder.Property(f => f.Key).IsRequired().HasMaxLength(120);
        builder.HasIndex(f => f.Key).IsUnique();
        builder.Property(f => f.Description).HasMaxLength(500);
        builder.Property(f => f.Enabled);

        builder.OwnsMany(f => f.Variants, b =>
        {
            b.ToTable("FeatureVariants");
            b.WithOwner().HasForeignKey("FeatureFlagId");
            b.HasKey(v => v.Id);
            b.Property(v => v.Id).ValueGeneratedNever();
            b.Property(v => v.Name).IsRequired().HasMaxLength(120);
            b.Property(v => v.Weight);
            b.Property(v => v.Value).HasMaxLength(500);
        });

        builder.Navigation(f => f.Variants).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
