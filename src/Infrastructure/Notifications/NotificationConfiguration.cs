using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

public sealed class NotificationConfiguration : IEntityTypeConfiguration<Notification>
{
    public void Configure(EntityTypeBuilder<Notification> builder)
    {
        builder.ToTable("Notifications");
        builder.HasKey(notification => notification.Id);
        builder.Property(notification => notification.Code).HasMaxLength(120).IsRequired();
        builder.Property(notification => notification.Message).HasMaxLength(2000).IsRequired();
        builder.Property(notification => notification.LinkUrl).HasMaxLength(500);
        builder.HasIndex(notification => new { notification.LearnerId, notification.CreatedAt });
        builder.HasIndex(notification => new { notification.LearnerId, notification.Code, notification.CreatedAt })
            .IsUnique();
    }
}
