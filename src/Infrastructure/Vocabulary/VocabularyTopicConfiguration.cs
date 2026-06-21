using Domain.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Vocabulary;

/// <summary>
/// EF Core mapping for the <see cref="VocabularyTopic"/> aggregate (the curated catalog plus its
/// lazily-filled passages). The target <see cref="TopicWord"/>s are an owned collection stored in
/// a side table; the slug is uniquely indexed and the level is indexed for catalog reads.
/// </summary>
public sealed class VocabularyTopicConfiguration : IEntityTypeConfiguration<VocabularyTopic>
{
    public void Configure(EntityTypeBuilder<VocabularyTopic> builder)
    {
        builder.ToTable("VocabularyTopics");
        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id).ValueGeneratedNever();

        builder.Property(t => t.Slug).IsRequired().HasMaxLength(120);
        builder.HasIndex(t => t.Slug).IsUnique();

        builder.Property(t => t.Title).IsRequired().HasMaxLength(200);
        builder.Property(t => t.TitleUz).IsRequired().HasMaxLength(200);
        builder.Property(t => t.Category).IsRequired().HasMaxLength(60);
        builder.Property(t => t.GrammarFocusCode).IsRequired().HasMaxLength(60).HasDefaultValue(string.Empty);
        builder.Property(t => t.Sequence).HasDefaultValue(0);
        builder.Property(t => t.Level).HasConversion<int>();
        // Catalog reads return the level's topics in their learning sequence (grammar progression).
        builder.HasIndex(t => new { t.Level, t.Sequence });

        builder.Property(t => t.Passage).IsRequired().HasMaxLength(4000);
        builder.Property(t => t.Status).HasConversion<int>();
        builder.Property(t => t.ContentVersion).HasDefaultValue(0);
        builder.Property(t => t.CreatedAt);

        builder.OwnsMany(t => t.Words, b =>
        {
            b.ToTable("VocabularyTopicWords");
            b.WithOwner().HasForeignKey("VocabularyTopicId");
            b.Property<int>("Id");
            b.HasKey("Id");

            b.Property(w => w.Word).IsRequired().HasMaxLength(160);
            b.Property(w => w.Translation).IsRequired().HasMaxLength(200);
            b.Property(w => w.ExampleSentence).HasMaxLength(2000);
            b.Property(w => w.PartOfSpeech).HasConversion<int>().HasDefaultValue(PartOfSpeech.Other);
            b.Property(w => w.LexicalCategory).HasConversion<int>();
            b.Property(w => w.Register).HasMaxLength(40);
            b.Property(w => w.UsageNote).HasMaxLength(500);
            b.Property(w => w.ImageUrl).HasMaxLength(800);
            b.Property(w => w.ImageSource).HasMaxLength(60);
            b.Property(w => w.ImageAttribution).HasMaxLength(300);
        });

        builder.Navigation(t => t.Words).UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
