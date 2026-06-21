using Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity;

/// <summary>
/// EF Core mapping for the <see cref="UserAccount"/> aggregate. The Google subject is the
/// unique natural key used to resolve returning logins; the id is the learner id and is
/// assigned in the domain (never database-generated).
/// </summary>
public sealed class UserAccountConfiguration : IEntityTypeConfiguration<UserAccount>
{
    public void Configure(EntityTypeBuilder<UserAccount> builder)
    {
        builder.ToTable("UserAccounts");
        builder.HasKey(u => u.Id);
        builder.Property(u => u.Id).ValueGeneratedNever();

        builder.Property(u => u.GoogleSubject).IsRequired().HasMaxLength(64);
        builder.HasIndex(u => u.GoogleSubject).IsUnique();

        // Case-sensitive at the DB level (Google's `email` claim already comes lowercased for
        // Gmail addresses); the application layer does the case-insensitive check before insert
        // (AuthenticateWithGoogleCommandHandler). This index is the race-condition backstop.
        builder.Property(u => u.Email).IsRequired().HasMaxLength(320);
        builder.HasIndex(u => u.Email).IsUnique();
        builder.Property(u => u.DisplayName).IsRequired().HasMaxLength(200);

        // Optional until the user picks one; normalized lowercase, unique. Postgres treats
        // multiple NULLs as distinct, so accounts without a handle don't collide on the index.
        builder.Property(u => u.Username).HasMaxLength(UsernameRules.MaxLength);
        builder.HasIndex(u => u.Username).IsUnique();

        // The name the AI tutor uses; optional until the learner answers the one-time prompt.
        builder.Property(u => u.PreferredName).HasMaxLength(UserAccount.MaxPreferredNameLength);

        builder.Property(u => u.PictureUrl).HasMaxLength(1000);

        builder.Property(u => u.CreatedAt);
        builder.Property(u => u.LastLoginAt);
        builder.Property(u => u.ProTrialExpiresAt);
        builder.Property(u => u.BirthDate);
        builder.Property(u => u.Gender).HasConversion<int?>();
        builder.Property(u => u.AcquisitionSource).HasConversion<int?>();
        builder.Property(u => u.AcquisitionSourceOther).HasMaxLength(100);
        builder.Property(u => u.DemographicsCompletedAt);

        // Admin-panel access granted by the super-admin; false for every account by default.
        builder.Property(u => u.IsAdmin).HasDefaultValue(false);
    }
}
