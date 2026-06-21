using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

/// <summary>EF Core mapping for <see cref="NotificationArrival"/> - keyed by the reminder occurrence key.</summary>
public sealed class NotificationArrivalConfiguration : IEntityTypeConfiguration<NotificationArrival>
{
    public void Configure(EntityTypeBuilder<NotificationArrival> builder)
    {
        builder.ToTable("NotificationArrivals");
        builder.HasKey(a => a.Key);
        builder.Property(a => a.Key).HasMaxLength(120).ValueGeneratedNever();
        builder.Property(a => a.FirstSeenAt);
    }
}
