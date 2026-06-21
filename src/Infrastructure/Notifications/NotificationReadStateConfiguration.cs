using Domain.Notifications;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Notifications;

/// <summary>
/// EF Core mapping for <see cref="NotificationReadState"/>. Keyed by the learner id (assigned in the
/// domain), one row per learner holding their "mark all read" watermark.
/// </summary>
public sealed class NotificationReadStateConfiguration : IEntityTypeConfiguration<NotificationReadState>
{
    public void Configure(EntityTypeBuilder<NotificationReadState> builder)
    {
        builder.ToTable("NotificationReadStates");
        builder.HasKey(s => s.LearnerId);
        builder.Property(s => s.LearnerId).ValueGeneratedNever();
        builder.Property(s => s.LastReadAt);
    }
}
