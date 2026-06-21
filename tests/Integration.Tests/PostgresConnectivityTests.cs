using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

/// <summary>
/// Smoke test that the EF Core / Npgsql wiring connects to a real PostgreSQL,
/// using Testcontainers. Tagged "Integration" so CI (which has no Docker daemon by
/// default) skips it; run locally with Docker available.
/// </summary>
[Trait("Category", "Integration")]
public sealed class PostgresConnectivityTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Db_context_can_connect_to_postgres()
    {
        var options = new DbContextOptionsBuilder<EnglishAiDbContext>()
            .UseNpgsql(_postgres.GetConnectionString())
            .Options;

        await using var context = new EnglishAiDbContext(options);

        (await context.Database.CanConnectAsync()).Should().BeTrue();
    }
}
