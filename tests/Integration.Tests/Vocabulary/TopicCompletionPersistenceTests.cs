using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Learning;
using Infrastructure.Persistence;
using Infrastructure.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests.Vocabulary;

/// <summary>
/// The P1 failure boundary, against a real PostgreSQL.
///
/// Every skill's submit handler (reading, listening, grammar, writing, speaking, vocabulary) writes
/// the learner profile and the topic completion record in the same request. While those were two
/// separate commits, a failure between them left the profile updated and the score lost - and the
/// learner got an HTTP 500 for work that had partly landed. These tests assert the staging contract
/// the handlers now rely on: nothing is visible until the single commit, and then everything is.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TopicCompletionPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 8, 16, 9, 0, 0, TimeSpan.Zero);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private DbContextOptions<EnglishAiDbContext> Options() =>
        new DbContextOptionsBuilder<EnglishAiDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

    [Fact]
    public async Task Deferred_profile_and_topic_completion_changes_commit_together()
    {
        var learnerId = Guid.NewGuid();
        var topicId = Guid.NewGuid();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
            await context.LearnerProfiles.AddAsync(LearnerProfile.CreateAtLevel(learnerId, CefrLevel.B1, Now));
            await context.SaveChangesAsync();
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var completions = new EfTopicCompletionStore(context);
            var profiles = new EfLearnerProfileRepository(context);

            var profile = await profiles.GetByLearnerIdAsync(learnerId, CancellationToken.None);
            profile!.RecordActivity(SkillType.Reading, 90, Now);
            var record = TopicCompletionRecord.Start(learnerId, topicId, CefrLevel.B1, Now);
            record.RecordModule(SkillType.Reading, 90, Now);

            await profiles.TrackAsync(profile, CancellationToken.None);
            await completions.TrackAsync(record, CancellationToken.None);

            // Nothing is durable yet: a failure here leaves the learner exactly as they were, with
            // no half-applied submission to reconcile.
            await using (var beforeCommit = new EnglishAiDbContext(Options()))
            {
                (await beforeCommit.TopicCompletionRecords.CountAsync()).Should().Be(0);
                var unchanged = await new EfLearnerProfileRepository(beforeCommit)
                    .GetByLearnerIdAsync(learnerId, CancellationToken.None);
                unchanged!.Activities.Should().BeEmpty();
            }

            await completions.CommitAsync(CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var saved = await context.TopicCompletionRecords.SingleAsync();
            saved.ScoreFor(SkillType.Reading).Should().Be(90);
            var savedProfile = await new EfLearnerProfileRepository(context)
                .GetByLearnerIdAsync(learnerId, CancellationToken.None);
            savedProfile!.Activities.Should().ContainSingle(activity =>
                activity.Skill == SkillType.Reading && activity.Score == 90);
        }
    }

    [Fact]
    public async Task Speaking_progress_and_completion_commit_together()
    {
        // The speaking turn stages spoken-time progress and the module score it earns. Committing
        // them separately could credit a learner's practice minutes without the score, or the reverse.
        var learnerId = Guid.NewGuid();
        var topicId = Guid.NewGuid();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var completions = new EfTopicCompletionStore(context);
            var speaking = new EfTopicSpeakingProgressStore(context);

            var progress = TopicSpeakingProgress.Start(learnerId, topicId, Now);
            progress.AddSpeaking(TimeSpan.FromMinutes(6), Now);
            var record = TopicCompletionRecord.Start(learnerId, topicId, CefrLevel.A2, Now);
            record.RecordModule(SkillType.Speaking, 88, Now);

            await speaking.TrackAsync(progress, CancellationToken.None);
            await completions.TrackAsync(record, CancellationToken.None);

            await using (var beforeCommit = new EnglishAiDbContext(Options()))
            {
                (await beforeCommit.TopicSpeakingProgress.CountAsync()).Should().Be(0);
                (await beforeCommit.TopicCompletionRecords.CountAsync()).Should().Be(0);
            }

            await completions.CommitAsync(CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            (await context.TopicSpeakingProgress.SingleAsync()).IsLearned.Should().BeTrue();
            (await context.TopicCompletionRecords.SingleAsync())
                .ScoreFor(SkillType.Speaking).Should().Be(88);
        }
    }

    [Fact]
    public async Task SaveAsync_still_commits_on_its_own_for_single_aggregate_callers()
    {
        // Most callers touch one aggregate and want it durable immediately; only the multi-aggregate
        // submit handlers stage. Both paths have to keep working.
        var learnerId = Guid.NewGuid();
        var topicId = Guid.NewGuid();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var record = TopicCompletionRecord.Start(learnerId, topicId, CefrLevel.A1, Now);
            record.RecordModule(SkillType.Vocabulary, 80, Now);
            await new EfTopicCompletionStore(context).SaveAsync(record, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            (await context.TopicCompletionRecords.SingleAsync())
                .ScoreFor(SkillType.Vocabulary).Should().Be(80);
        }
    }
}
