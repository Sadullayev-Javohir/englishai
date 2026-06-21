using Domain.Learning;
using FluentAssertions;
using Infrastructure.Analytics;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Exercises <see cref="EfStudyLogStore"/> against the real EF Core / PostgreSQL mapping
/// (migrations applied). Proves the one-row-per-(learner, day) upsert accumulates time and -
/// critically - that two concurrent heartbeats racing to create the same day's row do NOT surface
/// the unique-constraint (23505) duplicate-key error that previously produced a 500 on the
/// study-time endpoint: the losing insert is retried as an update instead, so no seconds are lost.
/// Tagged "Integration" so CI without a Docker daemon skips it.
/// </summary>
[Trait("Category", "Integration")]
public sealed class StudyLogStorePersistenceTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = new EnglishAiDbContext(Options());
        await context.Database.MigrateAsync();
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private DbContextOptions<EnglishAiDbContext> Options() =>
        new DbContextOptionsBuilder<EnglishAiDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

    [Fact]
    public async Task Repeated_heartbeats_accumulate_into_one_daily_row()
    {
        var learnerId = Guid.NewGuid();
        var day = new DateOnly(2026, 7, 3);

        // Each call uses its own context (as a scoped-per-request store would).
        await AddAsync(learnerId, day, SkillType.Reading, 60);
        await AddAsync(learnerId, day, SkillType.Reading, 30);
        await AddAsync(learnerId, day, SkillType.Speaking, 45);

        await using var context = new EnglishAiDbContext(Options());
        var records = await context.DailyStudyRecords
            .Where(r => r.LearnerId == learnerId && r.Day == day)
            .ToListAsync();

        records.Should().ContainSingle();
        records[0].ReadingSeconds.Should().Be(90);
        records[0].SpeakingSeconds.Should().Be(45);
    }

    [Fact]
    public async Task Concurrent_first_heartbeats_do_not_throw_duplicate_key()
    {
        var learnerId = Guid.NewGuid();
        var day = new DateOnly(2026, 7, 4);

        // Fire many first-heartbeats for the SAME (learner, day) at once. Several will read
        // "no row yet" and race to INSERT; the unique index rejects all but one. The store must
        // retry the losers as updates so every call succeeds and every second is credited.
        var tasks = Enumerable.Range(0, 8)
            .Select(_ => AddAsync(learnerId, day, SkillType.Vocabulary, 10))
            .ToArray();

        var act = async () => await Task.WhenAll(tasks);
        await act.Should().NotThrowAsync();

        await using var context = new EnglishAiDbContext(Options());
        var records = await context.DailyStudyRecords
            .Where(r => r.LearnerId == learnerId && r.Day == day)
            .ToListAsync();

        records.Should().ContainSingle("the (learner, day) row is unique");
        records[0].VocabularySeconds.Should().Be(80, "all eight 10-second heartbeats must be credited");
    }

    private async Task AddAsync(Guid learnerId, DateOnly day, SkillType skill, int seconds)
    {
        await using var context = new EnglishAiDbContext(Options());
        var store = new EfStudyLogStore(context);
        await store.AddStudyTimeAsync(learnerId, day, skill, seconds, CancellationToken.None);
    }
}
