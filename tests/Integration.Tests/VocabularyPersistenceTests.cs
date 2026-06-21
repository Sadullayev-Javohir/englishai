using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Persistence;
using Infrastructure.Vocabulary;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Round-trips a <see cref="VocabularyItem"/> through the real EF Core / PostgreSQL
/// mapping (migrations applied), proving the owned <see cref="ReviewSchedule"/> persists
/// inline and that the due-items query translates to SQL. Tagged "Integration" so CI
/// without a Docker daemon skips it.
/// </summary>
[Trait("Category", "Integration")]
public sealed class VocabularyPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset LearnedAt = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

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
    public async Task Vocabulary_item_with_owned_schedule_round_trips_and_due_query_works()
    {
        var learnerId = Guid.NewGuid();
        Guid itemId;

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();

            var item = VocabularyItem.Learn(learnerId, "innovation", "yangilik", LearnedAt,
                "An age of innovation.", VocabularySource.Speaking);
            itemId = item.Id;

            await new EfVocabularyRepository(context).SaveAsync(item, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var repo = new EfVocabularyRepository(context);
            var loaded = await repo.GetByIdAsync(itemId, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Word.Should().Be("innovation");
            loaded.Schedule.Stage.Should().Be(ReviewStage.Day3);
            loaded.Schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(3));

            // Due query: nothing due before day 3, the item due on day 3.
            var notYetDue = await repo.GetDueAsync(LearnedAt.AddDays(2), CancellationToken.None);
            notYetDue.Should().BeEmpty();

            var due = await repo.GetDueForLearnerAsync(learnerId, LearnedAt.AddDays(3), CancellationToken.None);
            due.Should().ContainSingle(v => v.Id == itemId);

            // Update path: pass the review and re-save.
            loaded.RecordReview(passed: true, LearnedAt.AddDays(3));
            await repo.SaveAsync(loaded, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var reloaded = await new EfVocabularyRepository(context)
                .GetByIdAsync(itemId, CancellationToken.None);

            reloaded!.Schedule.Stage.Should().Be(ReviewStage.Day7);
            reloaded.Schedule.NextReviewAt.Should().Be(LearnedAt.AddDays(7));
        }
    }

    [Fact]
    public async Task Vocabulary_stats_reader_projects_owned_schedule_without_translation_failure()
    {
        var learnerId = Guid.NewGuid();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
            var item = VocabularyItem.Learn(
                learnerId, "progress", "rivojlanish", LearnedAt,
                source: VocabularySource.Speaking);
            item.MakeDueForReview(LearnedAt.AddDays(3));
            await new EfVocabularyRepository(context).SaveAsync(item, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var stats = await new EfVocabularyStatsReader(context)
                .ReadAsync(learnerId, LearnedAt.AddDays(3), CancellationToken.None);

            stats.Total.Should().Be(1);
            stats.Learning.Should().Be(1);
            stats.Due.Should().Be(1);
            stats.StageBreakdown[ReviewStage.Day3].Should().Be(1);
        }
    }
}
