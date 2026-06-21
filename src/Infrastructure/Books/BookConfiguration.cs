using System.Text.Json;
using Domain.Books;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Books;

/// <summary>
/// EF Core mapping for the <see cref="Book"/> aggregate. Sections are an owned collection and each
/// section owns its comprehension questions, so a book persists as one unit (the library lives in
/// PostgreSQL). A question's options are stored as a JSON column. The CEFR level is indexed for the
/// per-level library query.
/// </summary>
public sealed class BookConfiguration : IEntityTypeConfiguration<Book>
{
    public void Configure(EntityTypeBuilder<Book> builder)
    {
        builder.ToTable("Books");
        builder.HasKey(b => b.Id);
        builder.Property(b => b.Id).ValueGeneratedNever();

        builder.Property(b => b.Title).IsRequired().HasMaxLength(200);
        builder.Property(b => b.TitleUz).IsRequired().HasMaxLength(200);
        builder.Property(b => b.Author).IsRequired().HasMaxLength(160);
        builder.Property(b => b.Synopsis).IsRequired().HasMaxLength(2000);
        builder.Property(b => b.Topic).IsRequired().HasMaxLength(60);
        builder.Property(b => b.Level).HasConversion<int>();
        builder.Property(b => b.CoverImageQuery).IsRequired().HasMaxLength(120);
        builder.Property(b => b.CoverImageUrl).HasMaxLength(1000);
        builder.Property(b => b.CoverAttribution).HasMaxLength(300);
        builder.Property(b => b.CreatedAt);
        builder.HasIndex(b => b.Level);

        // SectionCount/HasCover are computed; not persisted.
        builder.Ignore(b => b.SectionCount);
        builder.Ignore(b => b.HasCover);

        builder.OwnsMany(b => b.Sections, section =>
        {
            section.ToTable("BookSections");
            section.WithOwner().HasForeignKey("BookId");
            section.HasKey(s => s.Id);
            section.Property(s => s.Id).ValueGeneratedNever();
            section.Property(s => s.Order);
            section.Property(s => s.Title).IsRequired().HasMaxLength(200);
            section.Property(s => s.Body).IsRequired().HasColumnType("text");
            section.Property(s => s.Status).HasConversion<int>();
            section.Ignore(s => s.IsFilled);
            section.Ignore(s => s.WordCount);
            section.HasIndex("BookId", nameof(BookSection.Order));

            section.OwnsMany(s => s.Questions, question =>
            {
                question.ToTable("BookComprehensionQuestions");
                question.WithOwner().HasForeignKey("BookSectionId");
                question.HasKey(q => q.Id);
                question.Property(q => q.Id).ValueGeneratedNever();
                question.Property(q => q.Prompt).IsRequired().HasMaxLength(2000);
                question.Property(q => q.CorrectOptionIndex);
                question.Property(q => q.Explanation).HasMaxLength(2000);

                question.Property(q => q.Options)
                    .HasConversion(
                        options => JsonSerializer.Serialize(options, (JsonSerializerOptions?)null),
                        json => JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null) ?? new List<string>())
                    .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                        (a, c) => a!.SequenceEqual(c!),
                        v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                        v => v.ToList()));
            });

            section.Navigation(s => s.Questions).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        builder.Navigation(b => b.Sections).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
