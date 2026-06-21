using Domain.Developer;
using Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Developer;

public sealed class DeveloperApiKeyConfiguration : IEntityTypeConfiguration<DeveloperApiKey>
{
    public void Configure(EntityTypeBuilder<DeveloperApiKey> builder)
    {
        builder.ToTable("DeveloperApiKeys");
        builder.HasKey(k => k.Id);
        builder.Property(k => k.Id).ValueGeneratedNever();
        builder.Property(k => k.Name).IsRequired().HasMaxLength(DeveloperApiKey.MaxNameLength);
        builder.Property(k => k.Prefix).IsRequired().HasMaxLength(DeveloperApiKey.PrefixLength);
        builder.Property(k => k.KeyHash).IsRequired().HasMaxLength(64);
        builder.HasIndex(k => k.KeyHash).IsUnique();
        builder.HasIndex(k => new { k.Prefix, k.RevokedAt });
        builder.HasIndex(k => k.UserId);
        builder.Ignore(k => k.IsActive);

        builder.HasOne<UserAccount>()
            .WithMany()
            .HasForeignKey(k => k.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
