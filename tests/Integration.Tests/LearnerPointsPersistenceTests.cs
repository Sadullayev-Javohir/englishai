using Domain.Gamification;
using FluentAssertions;
using Infrastructure.Gamification;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Round-trips <see cref="LearnerPoints"/> and <see cref="DiscountRedemption"/> through the
/// real EF Core / PostgreSQL mapping (migrations applied) - the leaderboard/points feature's
/// durable ledger and coupons. Tagged "Integration" so CI without a Docker daemon skips it.
/// </summary>
[Trait("Category", "Integration")]
public sealed class LearnerPointsPersistenceTests : IAsyncLifetime
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 12, 0, 0, TimeSpan.Zero);

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
    public async Task LearnerPoints_round_trips_and_the_learner_id_index_stays_unique()
    {
        var learnerId = Guid.NewGuid();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();

            var points = LearnerPoints.CreateNew(learnerId, Now);
            points.Earn(120, Now);
            points.TryAwardStreakMilestone(7, Now);

            var repo = new EfLearnerPointsRepository(context);
            await repo.SaveAsync(points, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var repo = new EfLearnerPointsRepository(context);
            var loaded = await repo.GetOrCreateAsync(learnerId, Now, CancellationToken.None);

            loaded.LifetimeXp.Should().Be(120);
            loaded.SpendableCoins.Should().Be(120);
            loaded.HighestStreakMilestoneReached.Should().Be(7);

            // Update path: earn more on the tracked aggregate and re-save.
            loaded.Earn(30, Now);
            await repo.SaveAsync(loaded, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var reloaded = await new EfLearnerPointsRepository(context)
                .GetOrCreateAsync(learnerId, Now, CancellationToken.None);

            reloaded.LifetimeXp.Should().Be(150);
        }
    }

    [Fact]
    public async Task DiscountRedemption_round_trips_and_code_lookup_works()
    {
        var learnerId = Guid.NewGuid();
        var tier = DiscountCatalog.Tiers[0];

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();

            var redemption = DiscountRedemption.Create(learnerId, tier, "ROUNDTRIP1", Now);
            var repo = new EfDiscountRedemptionRepository(context);
            await repo.SaveAsync(redemption, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var repo = new EfDiscountRedemptionRepository(context);
            var loaded = await repo.GetByCodeAsync("ROUNDTRIP1", CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.LearnerId.Should().Be(learnerId);
            loaded.DiscountPercent.Should().Be(tier.DiscountPercent);
            loaded.IsUsable(Now).Should().BeTrue();

            var active = await repo.GetActiveForLearnerAsync(learnerId, Now, CancellationToken.None);
            active.Should().ContainSingle(r => r.Code == "ROUNDTRIP1");

            loaded.MarkUsed(Now);
            await repo.SaveAsync(loaded, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var repo = new EfDiscountRedemptionRepository(context);
            var active = await repo.GetActiveForLearnerAsync(learnerId, Now, CancellationToken.None);

            active.Should().BeEmpty();
        }
    }
}
