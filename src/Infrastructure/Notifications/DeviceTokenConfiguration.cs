using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

/// <summary>EF Core mapping for <see cref="DeviceToken"/> - the token itself is unique (upsert key).</summary>
public sealed class DeviceTokenConfiguration : IEntityTypeConfiguration<DeviceToken>
{
    public void Configure(EntityTypeBuilder<DeviceToken> builder)
    {
        builder.ToTable("DeviceTokens");
        builder.HasKey(d => d.Id);
        builder.Property(d => d.Token).HasMaxLength(4096).IsRequired();
        builder.Property(d => d.Platform).HasMaxLength(16).IsRequired();
        builder.Property(d => d.UserAccountId).IsRequired();
        builder.Property(d => d.CreatedAt);
        builder.Property(d => d.LastSeenAt);
        builder.HasIndex(d => d.Token).IsUnique();
        builder.HasIndex(d => d.UserAccountId);
    }
}
