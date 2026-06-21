using Domain.Assistant;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Assistant.Sessions;

public sealed class AssistantSessionConfiguration : IEntityTypeConfiguration<AssistantSession>
{
    public void Configure(EntityTypeBuilder<AssistantSession> builder)
    {
        builder.ToTable("AssistantSessions");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Skill).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResourceType).HasMaxLength(32).IsRequired();
        builder.Property(x => x.ResourceId).HasMaxLength(200);
        builder.Property(x => x.Title).HasMaxLength(200).IsRequired();
        builder.HasIndex(x => new { x.LearnerId, x.ExpiresAt });
        builder.HasIndex(x => new { x.LearnerId, x.Skill, x.ResourceType, x.ResourceId, x.UpdatedAt });
        builder.HasMany(x => x.Messages).WithOne().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(x => x.Messages).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}

public sealed class AssistantMessageConfiguration : IEntityTypeConfiguration<AssistantMessage>
{
    public void Configure(EntityTypeBuilder<AssistantMessage> builder)
    {
        builder.ToTable("AssistantMessages");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        builder.Property(x => x.Role).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Text).HasMaxLength(16000).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(16).IsRequired();
        builder.Property(x => x.Source).HasMaxLength(32).IsRequired();
        builder.Property(x => x.SourceMetadataJson).HasColumnType("jsonb").HasDefaultValue("[]").IsRequired();
        builder.HasIndex(x => new { x.SessionId, x.CreatedAt });
        builder.HasIndex(x => new { x.SessionId, x.ClientRequestId, x.Role }).IsUnique().HasFilter("\"ClientRequestId\" IS NOT NULL");
    }
}
