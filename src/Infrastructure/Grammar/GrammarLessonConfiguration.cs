using System.Text.Json;
using Domain.Grammar;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Grammar;

/// <summary>
/// EF Core mapping for the <see cref="GrammarLesson"/> aggregate. The exercises and
/// application tasks are owned collections so they persist as one unit with the lesson; an
/// exercise's options are stored as a JSON column. The CEFR level and error category are
/// indexed for the adaptive, priority-ordered catalog query.
/// </summary>
public sealed class GrammarLessonConfiguration : IEntityTypeConfiguration<GrammarLesson>
{
    public void Configure(EntityTypeBuilder<GrammarLesson> builder)
    {
        builder.ToTable("GrammarLessons");
        builder.HasKey(l => l.Id);
        builder.Property(l => l.Id).ValueGeneratedNever();

        builder.Property(l => l.VocabularyTopicId);
        builder.HasIndex(l => l.VocabularyTopicId);
        builder.Property(l => l.GrammarFocusCode).HasMaxLength(60);
        builder.Property(l => l.Topic).IsRequired().HasMaxLength(200);
        builder.Property(l => l.Category).HasConversion<int>();
        builder.Property(l => l.Level).HasConversion<int>();
        // ContextIntro/ExplanationCode are empty on a pending topic-scoped shell until generation
        // fills them, so they are not required at the database level.
        builder.Property(l => l.ContextIntro).IsRequired().HasMaxLength(2000);
        builder.Property(l => l.ExplanationCode).IsRequired().HasMaxLength(80);
        builder.Property(l => l.Explanation).HasMaxLength(2000);
        builder.Property(l => l.CuratedTitleUz).HasMaxLength(300);
        builder.Property(l => l.CuratedSummaryUz).HasMaxLength(4000);
        builder.Property(l => l.CuratedFormulas)
            .HasColumnType("jsonb")
            .HasConversion(
                value => JsonSerializer.Serialize(value, (JsonSerializerOptions?)null),
                value => DeserializeStringList(value))
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                (a, b) => a!.SequenceEqual(b!),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item.GetHashCode())),
                value => value.ToList()));
        builder.Property(l => l.Status).HasConversion<int>();
        builder.Property(l => l.CreatedAt);
        builder.HasIndex(l => l.Level);
        builder.HasIndex(l => l.Category);

        builder.OwnsMany(l => l.Exercises, b =>
        {
            b.ToTable("GrammarExercises");
            b.WithOwner().HasForeignKey("GrammarLessonId");
            b.HasKey(e => e.Id);
            b.Property(e => e.Id).ValueGeneratedNever();
            b.Property(e => e.Type).HasConversion<int>();
            b.Property(e => e.Prompt).IsRequired().HasMaxLength(2000);
            b.Property(e => e.CorrectOptionIndex);
            b.Property(e => e.HintCode).HasMaxLength(80);
            b.Property(e => e.Explanation).HasMaxLength(2000);

            b.Property(e => e.Options)
                .HasConversion(
                    options => JsonSerializer.Serialize(options, (JsonSerializerOptions?)null),
                    json => DeserializeStringList(json))
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (a, c) => a!.SequenceEqual(c!),
                    v => v.Aggregate(0, (hash, s) => HashCode.Combine(hash, s.GetHashCode())),
                    v => v.ToList()));
        });

        builder.OwnsMany(l => l.ApplicationTasks, b =>
        {
            b.ToTable("GrammarApplicationTasks");
            b.WithOwner().HasForeignKey("GrammarLessonId");
            b.HasKey(t => t.Id);
            b.Property(t => t.Id).ValueGeneratedNever();
            b.Property(t => t.TargetSkill).HasConversion<int>();
            b.Property(t => t.Prompt).IsRequired().HasMaxLength(2000);
        });

        builder.OwnsMany(l => l.CuratedRules, b =>
        {
            b.ToTable("GrammarCuratedRules");
            b.WithOwner().HasForeignKey("GrammarLessonId");
            b.HasKey(item => item.Id);
            b.Property(item => item.Id).ValueGeneratedNever();
            b.Property(item => item.HeadingUz).IsRequired().HasMaxLength(300);
            b.Property(item => item.BodyUz).IsRequired().HasMaxLength(4000);
        });

        builder.Navigation(l => l.Exercises).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(l => l.ApplicationTasks).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(l => l.CuratedRules).UsePropertyAccessMode(PropertyAccessMode.Field);
    }

    internal static List<string> DeserializeStringList(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return new List<string>();

        try
        {
            return JsonSerializer.Deserialize<List<string>>(json, (JsonSerializerOptions?)null)
                ?? new List<string>();
        }
        catch (JsonException)
        {
            return new List<string>();
        }
    }
}
