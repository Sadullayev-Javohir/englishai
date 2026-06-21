using Domain.Competition;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Infrastructure.Competition;

/// <summary>
/// EF Core mapping for the <see cref="Domain.Competition.Competition"/> aggregate. Slides and Participants are owned
/// collections; the access code is stored only as a salted hash (rule 13). Per-slide answers are
/// owned by their participant.
/// </summary>
public sealed class CompetitionConfiguration : IEntityTypeConfiguration<Domain.Competition.Competition>
{
    public void Configure(EntityTypeBuilder<Domain.Competition.Competition> builder)
    {
        builder.ToTable("Competitions");
        builder.HasKey(c => c.Id);
        builder.Property(c => c.Id).ValueGeneratedNever();

        builder.Property(c => c.Title).IsRequired().HasMaxLength(120);
        builder.Property(c => c.HostLearnerId).IsRequired();
        builder.Property(c => c.Status).HasConversion<int>();
        builder.Property(c => c.AccessCodeHash).IsRequired().HasMaxLength(128);
        builder.Property(c => c.AccessCodeSalt).IsRequired().HasMaxLength(64);
        builder.Property(c => c.CurrentSlideIndex).HasDefaultValue(-1);
        builder.Property(c => c.CreatedAt).IsRequired();
        builder.Property(c => c.StartedAt);
        builder.Property(c => c.FinishedAt);

        // Topic ids - stored as a JSON array on the competition (simple, read-only list).
        builder.Property(c => c.TopicIds)
            .HasConversion(
                v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                v => System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<Guid>())
            .HasColumnType("text")
            .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<Guid>>(
                (left, right) => left != null && right != null && left.SequenceEqual(right),
                value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                value => value.ToList()));

        // Settings - a value object stored as a JSON column.
        builder.OwnsOne(c => c.Settings, s =>
        {
            s.Property(x => x.SlideDurationSeconds).HasColumnName("Settings_SlideDurationSeconds");
            s.Property(x => x.AutoAdvance).HasColumnName("Settings_AutoAdvance");
            s.Property(x => x.ShuffleSlides).HasColumnName("Settings_ShuffleSlides");
            s.Property(x => x.AllowLateJoin).HasColumnName("Settings_AllowLateJoin");
            s.Property(x => x.QuestionsPerTopic).HasColumnName("Settings_QuestionsPerTopic");
            s.Property(x => x.ShowLiveLeaderboard).HasColumnName("Settings_ShowLiveLeaderboard");
            s.Property(x => x.BasePointsPerCorrect).HasColumnName("Settings_BasePointsPerCorrect");
        });

        builder.Navigation(c => c.Slides).UsePropertyAccessMode(PropertyAccessMode.Field);
        builder.Navigation(c => c.Participants).UsePropertyAccessMode(PropertyAccessMode.Field);

        builder.OwnsMany(c => c.Slides, sb =>
        {
            sb.ToTable("CompetitionSlides");
            sb.WithOwner().HasForeignKey("CompetitionId");
            sb.HasKey(s => s.Id);
            sb.Property(s => s.Id).ValueGeneratedNever().HasColumnName("Id");
            sb.Property(s => s.Order).IsRequired();
            sb.Property(s => s.SourceType).HasConversion<int>();
            sb.Property(s => s.SourceTopicId).IsRequired();
            sb.Property(s => s.QuestionText).IsRequired().HasMaxLength(2000);
            sb.Property(s => s.Options)
                .HasConversion(
                    v => System.Text.Json.JsonSerializer.Serialize(v, (System.Text.Json.JsonSerializerOptions?)null),
                    v => System.Text.Json.JsonSerializer.Deserialize<List<string>>(v, (System.Text.Json.JsonSerializerOptions?)null) ?? new List<string>())
                .HasColumnType("text")
                .Metadata.SetValueComparer(new ValueComparer<IReadOnlyList<string>>(
                    (left, right) => left != null && right != null && left.SequenceEqual(right),
                    value => value.Aggregate(0, (hash, item) => HashCode.Combine(hash, item)),
                    value => value.ToList()));
            sb.Property(s => s.CorrectOptionIndex).IsRequired();
            sb.Property(s => s.Points).IsRequired();
        });

        builder.OwnsMany(c => c.Participants, pb =>
        {
            pb.ToTable("CompetitionParticipants");
            pb.WithOwner().HasForeignKey("CompetitionId");
            pb.HasKey(p => p.Id);
            pb.Property(p => p.Id).ValueGeneratedNever().HasColumnName("Id");
            pb.Property(p => p.LearnerId).IsRequired();
            pb.Property(p => p.DisplayName).IsRequired().HasMaxLength(60);
            pb.Property(p => p.IsHost).HasDefaultValue(false);
            pb.Property(p => p.Status).HasConversion<int>().HasDefaultValue(ParticipantStatus.Joined);
            pb.Property(p => p.Score).HasDefaultValue(0);
            pb.Property(p => p.JoinedAt).IsRequired();

            pb.OwnsMany(p => p.Answers, ab =>
            {
                ab.ToTable("CompetitionAnswers");
                ab.WithOwner().HasForeignKey("ParticipantId");
                ab.HasKey(a => a.Id);
                ab.Property(a => a.SlideId).IsRequired();
                ab.Property(a => a.SelectedOptionIndex).IsRequired();
                ab.Property(a => a.IsCorrect).IsRequired();
                ab.Property(a => a.PointsAwarded).IsRequired();
                ab.Property(a => a.AnsweredAt).IsRequired();
            });
        });
    }
}
