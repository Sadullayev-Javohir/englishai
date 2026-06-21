using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

/// <summary>EF Core mapping for <see cref="AdminBroadcast"/> - one row per operator-sent broadcast.</summary>
public sealed class AdminBroadcastConfiguration : IEntityTypeConfiguration<AdminBroadcast>
{
    public void Configure(EntityTypeBuilder<AdminBroadcast> builder)
    {
        builder.ToTable("AdminBroadcasts");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();
        builder.Property(b => b.Title).HasMaxLength(120).IsRequired();
        builder.Property(b => b.Body).HasMaxLength(500).IsRequired();
        builder.Property(b => b.LinkUrl).HasMaxLength(200);
        builder.Property(b => b.CreatedAt);
        builder.Property(b => b.CreatedByUserId);
        builder.HasIndex(b => b.CreatedAt);
    }
}
