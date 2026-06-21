using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

/// <summary>
/// EF Core mapping for <see cref="NotificationDismissal"/>. Composite key (learner + notification) so a
/// learner can dismiss many notifications and each pair is recorded at most once.
/// </summary>
public sealed class NotificationDismissalConfiguration : IEntityTypeConfiguration<NotificationDismissal>
{
    public void Configure(EntityTypeBuilder<NotificationDismissal> builder)
    {
        builder.ToTable("NotificationDismissals");
        builder.HasKey(d => new { d.LearnerId, d.NotificationId });
        builder.Property(d => d.LearnerId).ValueGeneratedNever();
        builder.Property(d => d.NotificationId).ValueGeneratedNever();
        builder.Property(d => d.DismissedAt);
    }
}
