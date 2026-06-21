using Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity;

/// <summary>
/// EF Core mapping for <see cref="UserPreferences"/>. Keyed by the user id (which is the
/// account/learner id, assigned in the domain). Enums persist as their int codes, matching
/// the wire contract the SPA uses.
/// </summary>
public sealed class UserPreferencesConfiguration : IEntityTypeConfiguration<UserPreferences>
{
    public void Configure(EntityTypeBuilder<UserPreferences> builder)
    {
        builder.ToTable("UserPreferences");
        builder.HasKey(p => p.UserId);
        builder.Property(p => p.UserId).ValueGeneratedNever();

        builder.Property(p => p.DailyGoal).HasConversion<int>();
        builder.Property(p => p.LanguageBalance).HasConversion<int>();
        builder.Property(p => p.EmailNotifications);
        builder.Property(p => p.PushNotifications);
        builder.Property(p => p.UpdatedAt);
    }
}
