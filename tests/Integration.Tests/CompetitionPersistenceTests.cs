using Domain.Competition;
using FluentAssertions;
using Infrastructure.Competition;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Proves the complete competition aggregate is represented by migrations and survives a fresh
/// DbContext, which is equivalent to a process restart against PostgreSQL.
/// </summary>
[Trait("Category", "Integration")]
public sealed class CompetitionPersistenceTests : IAsyncLifetime
{
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
    public async Task Competition_aggregate_round_trips_through_migrated_postgres()
    {
        var hostId = Guid.NewGuid();
        var topicId = Guid.NewGuid();
        var competition = Competition.Create(
            hostId,
            "Host",
            "Durable competition",
            new CompetitionSettings(),
            new[] { topicId });
        competition.Join(hostId, "Host");

        await using (var context = new EnglishAiDbContext(Options()))
        {
            await context.Database.MigrateAsync();
            await new EfCompetitionRepository(context).AddAsync(competition, CancellationToken.None);
        }

        await using (var context = new EnglishAiDbContext(Options()))
        {
            var loaded = await new EfCompetitionRepository(context)
                .GetByIdAsync(competition.Id, CancellationToken.None);

            loaded.Should().NotBeNull();
            loaded!.Title.Should().Be("Durable competition");
            loaded.Status.Should().Be(CompetitionStatus.Lobby);
            loaded.TopicIds.Should().ContainSingle().Which.Should().Be(topicId);
            loaded.Participants.Should().ContainSingle();
            loaded.Participants[0].LearnerId.Should().Be(hostId);
            loaded.Settings.SlideDurationSeconds.Should().Be(CompetitionSettings.DefaultSlideDurationSeconds);
        }
    }
}
