using Domain.Curriculum;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Curriculum;

public sealed class CurriculumGenerationRunConfiguration : IEntityTypeConfiguration<CurriculumGenerationRun>
{
    public void Configure(EntityTypeBuilder<CurriculumGenerationRun> b)
    {
        b.ToTable("CurriculumGenerationRuns"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Version).HasMaxLength(30); b.Property(x => x.Provider).HasMaxLength(60);
        b.Property(x => x.Model).HasMaxLength(100); b.Property(x => x.Status).HasConversion<int>();
        b.HasIndex(x => new { x.Version, x.Status });
    }
}

public sealed class CurriculumGenerationItemConfiguration : IEntityTypeConfiguration<CurriculumGenerationItem>
{
    public void Configure(EntityTypeBuilder<CurriculumGenerationItem> b)
    {
        b.ToTable("CurriculumGenerationItems"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.Property(x => x.Module).HasMaxLength(30); b.Property(x => x.SubjectKey).HasMaxLength(180);
        b.Property(x => x.PromptVersion).HasMaxLength(30); b.Property(x => x.Status).HasConversion<int>();
        b.Property(x => x.Level).HasConversion<int?>(); b.Property(x => x.PayloadJson).HasColumnType("jsonb");
        b.Property(x => x.ValidationJson).HasColumnType("jsonb"); b.Property(x => x.ErrorCode).HasMaxLength(80);
        b.Property(x => x.ErrorMessage).HasMaxLength(1000); b.Property(x => x.LeaseOwner).HasMaxLength(120);
        b.HasIndex(x => new { x.RunId, x.Module, x.SubjectKey }).IsUnique();
        b.HasIndex(x => new { x.RunId, x.Status, x.NextAttemptAt });
    }
}

public sealed class TopicSpeakingBlueprintConfiguration : IEntityTypeConfiguration<TopicSpeakingBlueprint>
{
    public void Configure(EntityTypeBuilder<TopicSpeakingBlueprint> b)
    {
        b.ToTable("TopicSpeakingBlueprints"); b.HasKey(x => x.Id); b.Property(x => x.Id).ValueGeneratedNever();
        b.HasIndex(x => x.TopicId).IsUnique(); b.Property(x => x.Level).HasConversion<int>();
        b.Property(x => x.Objective).HasMaxLength(1000); b.Property(x => x.PrimaryGrammarFocus).HasMaxLength(80);
        b.Property(x => x.ReviewGrammarFocus).HasMaxLength(80); b.Property(x => x.PriorityWordsJson).HasColumnType("jsonb");
        b.Property(x => x.QuestionsJson).HasColumnType("jsonb"); b.Property(x => x.RubricJson).HasColumnType("jsonb");
    }
}
