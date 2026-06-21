using Domain.Gamification;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Gamification;

/// <summary>
/// EF Core mapping for <see cref="EnergySpend"/>. The unique
/// (<c>LearnerId</c>, <c>Action</c>, <c>ReferenceId</c>) index is what makes spending idempotent:
/// it is the database-level guarantee that re-opening a video or reconnecting to a conversation
/// can never debit the bar twice.
/// </summary>
public sealed class EnergySpendConfiguration : IEntityTypeConfiguration<EnergySpend>
{
    public void Configure(EntityTypeBuilder<EnergySpend> builder)
    {
        builder.ToTable("EnergySpends");
        builder.HasKey(s => s.Id);
        builder.Property(s => s.Id).ValueGeneratedNever();

        builder.Property(s => s.LearnerId).IsRequired();
        builder.Property(s => s.Action).IsRequired().HasConversion<int>();
        builder.Property(s => s.ReferenceId).IsRequired().HasMaxLength(128);
        builder.Property(s => s.SpentAt).IsRequired();

        builder.HasIndex(s => new { s.LearnerId, s.Action, s.ReferenceId }).IsUnique();
    }
}
