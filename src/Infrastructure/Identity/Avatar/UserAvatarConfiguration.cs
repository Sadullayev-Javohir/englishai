using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Identity.Avatar;

public sealed class UserAvatarConfiguration : IEntityTypeConfiguration<UserAvatar>
{
    public void Configure(EntityTypeBuilder<UserAvatar> builder)
    {
        builder.ToTable("UserAvatars");
        builder.HasKey(x => x.UserId);
        builder.Property(x => x.UserId).ValueGeneratedNever();
        builder.Property(x => x.Data);
        builder.Property(x => x.ContentType).IsRequired().HasMaxLength(100);
        builder.Property(x => x.ObjectKey).HasMaxLength(512);
        builder.Property(x => x.Checksum).HasMaxLength(64);
        builder.Property(x => x.ObjectETag).HasMaxLength(160);
        builder.Property(x => x.UpdatedAt);
    }
}
