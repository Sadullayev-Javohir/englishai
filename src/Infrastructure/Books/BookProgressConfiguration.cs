using Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Books;

/// <summary>
/// EF Core mapping for <see cref="BookProgress"/>. A unique (learner, book) index keeps it one row
/// per pair; the per-section best scores are an owned collection in a side table so the aggregate
/// loads and saves as a unit.
/// </summary>
public sealed class BookProgressConfiguration : IEntityTypeConfiguration<BookProgress>
{
    public void Configure(EntityTypeBuilder<BookProgress> builder)
    {
        builder.ToTable("BookProgress");
        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.LearnerId).IsRequired();
        builder.Property(p => p.BookId).IsRequired();
        builder.HasIndex(p => new { p.LearnerId, p.BookId }).IsUnique();
        builder.HasIndex(p => p.LearnerId);

        builder.Property(p => p.CompletedAt);
        builder.Property(p => p.CreatedAt);
        builder.Property(p => p.UpdatedAt);

        builder.Ignore(p => p.PassedSectionCount);
        builder.Ignore(p => p.IsCompleted);

        builder.OwnsMany(p => p.Sections, scores =>
        {
            scores.ToTable("BookSectionScores");
            scores.WithOwner().HasForeignKey("BookProgressId");
            scores.HasKey("BookProgressId", nameof(BookSectionScore.SectionId));

            scores.Property(s => s.SectionId);
            scores.Property(s => s.BestCorrectCount);
            scores.Property(s => s.Passed);
            scores.Property(s => s.AchievedAt);
        });

        builder.Navigation(p => p.Sections).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
