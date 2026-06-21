using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Round-trips a <see cref="TopicCompletionRecord"/> (PROJECT-SPEC K.5) through the real EF Core /
/// PostgreSQL mapping (migrations applied), proving the owned <see cref="TopicModuleScore"/>
/// collection persists in its side table and that the mastered-topics query translates to SQL.
/// Tagged "Integration" so CI without a Docker daemon skips it.
/// </summary>
[Trait("Category", "Integration")]
public sealed class TopicCompletionPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 6, 23, 9, 0, 0, TimeSpan.Zero);

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
    public async Task Completion_record_with_owned_module_scores_round_trips_and_mastery_query_works()
    {
        var learnerId = Guid.NewGuid();
        var topicId = Guid.NewGuid();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();

            var record = TopicCompletionRecord.Start(learnerId, topicId, CefrLevel.B1, Now);
            record.RecordModule(SkillType.Vocabulary, 90, Now);
            record.RecordModule(SkillType.Grammar, 50, Now);

            await new EfTopicCompletionStore(context).SaveAsync(record, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var store = new EfTopicCompletionStore(context);
            var loaded = await store.GetAsync(learnerId, topicId, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Level.Should().Be(CefrLevel.B1);
            loaded.IsMastered.Should().BeFalse();
            loaded.ScoreFor(SkillType.Vocabulary).Should().Be(90);
            loaded.IsModulePassed(SkillType.Grammar).Should().BeFalse();
            loaded.ModuleScores.Should().HaveCount(2);

            // No topic mastered yet.
            (await store.GetMasteredTopicIdsAsync(learnerId, CancellationToken.None)).Should().BeEmpty();

            // Pass the remaining modules and re-save.
            foreach (var module in TopicCompletionRecord.RequiredModules)
                loaded.RecordModule(module, 80, Now.AddDays(1));

            loaded.IsMastered.Should().BeTrue();
            await store.SaveAsync(loaded, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var store = new EfTopicCompletionStore(context);
            var reloaded = await store.GetAsync(learnerId, topicId, CancellationToken.None);

            reloaded!.IsMastered.Should().BeTrue();
            reloaded.PassedModuleCount.Should().Be(6);
            // Best score is kept: Vocabulary's 90 must not be lowered by the later 80.
            reloaded.ScoreFor(SkillType.Vocabulary).Should().Be(90);

            var mastered = await store.GetMasteredTopicIdsAsync(learnerId, CancellationToken.None);
            mastered.Should().ContainSingle().Which.Should().Be(topicId);
        }
    }
}
