using Domain.Assessment;
using Domain.Learning;
using FluentAssertions;
using Infrastructure.Learning;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Round-trips a <see cref="LearnerProfile"/> aggregate through the real EF Core /
/// PostgreSQL mapping (migrations applied), proving the owned seed/activity/error
/// collections persist and reload as one unit. Tagged "Integration" so CI without a
/// Docker daemon skips it.
/// </summary>
[Trait("Category", "Integration")]
public sealed class LearnerProfilePersistenceTests : IAsyncLifetime
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
    public async Task Profile_with_owned_collections_round_trips()
    {
        var learnerId = Guid.NewGuid();

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();

            var placement = new PlacementResult(CefrLevel.B1, CefrLevel.B1.ToScore(),
                new Dictionary<TestStage, StageResult>
                {
                    [TestStage.Reading] = new(TestStage.Reading, CefrLevel.B2, CefrLevel.B2.ToScore())
                });

            var profile = LearnerProfile.CreateFromPlacement(learnerId, placement, Now);
            profile.RecordActivity(SkillType.Speaking, 88, Now);
            profile.RecordError(ErrorCategory.Articles, SkillType.Speaking, Now);

            var repo = new EfLearnerProfileRepository(context);
            await repo.SaveAsync(profile, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var repo = new EfLearnerProfileRepository(context);
            var loaded = await repo.GetByLearnerIdAsync(learnerId, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.OverallLevel.Should().Be(CefrLevel.B1);
            loaded.Seeds.Should().HaveCount(6);
            loaded.Activities.Should().ContainSingle(a => a.Skill == SkillType.Speaking && a.Score == 88);
            loaded.Errors.Should().ContainSingle(e => e.Category == ErrorCategory.Articles);

            // Update path: record more activity on the tracked aggregate and re-save.
            loaded.RecordActivity(SkillType.Reading, 70, Now);
            await repo.SaveAsync(loaded, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var reloaded = await new EfLearnerProfileRepository(context)
                .GetByLearnerIdAsync(learnerId, CancellationToken.None);

            reloaded!.Activities.Should().HaveCount(2);
        }
    }
}
