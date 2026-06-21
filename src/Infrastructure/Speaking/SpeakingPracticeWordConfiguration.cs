using Domain.Speaking;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Speaking;

public sealed class SpeakingPracticeWordConfiguration : IEntityTypeConfiguration<SpeakingPracticeWord>
{
    public void Configure(EntityTypeBuilder<SpeakingPracticeWord> builder)
    {
        builder.ToTable("SpeakingPracticeWords");
        builder.HasKey(word => word.Id);
        builder.Property(word => word.Id).ValueGeneratedNever();
        builder.Property(word => word.LearnerId).IsRequired();
        builder.Property(word => word.Word).IsRequired().HasMaxLength(100);
        builder.Property(word => word.NormalizedWord).IsRequired().HasMaxLength(100);
        builder.Property(word => word.LastErrorType).HasConversion<int>();
        builder.HasIndex(word => new { word.LearnerId, word.NormalizedWord }).IsUnique();
        builder.HasIndex(word => new { word.LearnerId, word.MasteredAt, word.LastFailedAt });
    }
}
