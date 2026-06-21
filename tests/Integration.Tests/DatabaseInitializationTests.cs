using FluentAssertions;
using Infrastructure.Diagnostics;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

[Trait("Category", "Integration")]
public sealed class DatabaseInitializationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public Task InitializeAsync() => _postgres.StartAsync();

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Empty_database_is_migrated_seeded_and_is_ready()
    {
        await using var provider = CreateProvider();

        var result = await DatabaseInitializer.InitializeAsync(provider);

        result.PendingMigrations.Should().NotBeEmpty();
        result.SeededCatalog.Should().BeTrue();
        var health = await CreateSchemaHealthCheck(provider).CheckHealthAsync(new HealthCheckContext());
        health.Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Up_to_date_database_is_idempotent()
    {
        await using var provider = CreateProvider();
        await DatabaseInitializer.InitializeAsync(provider);

        var second = await DatabaseInitializer.InitializeAsync(provider);

        second.PendingMigrations.Should().BeEmpty();
        second.SeededCatalog.Should().BeFalse();
    }

    [Fact]
    public async Task Pending_database_fails_readiness_until_migration_runs()
    {
        await using var provider = CreateProvider();
        var check = CreateSchemaHealthCheck(provider);

        (await check.CheckHealthAsync(new HealthCheckContext())).Status.Should().Be(HealthStatus.Unhealthy);
        await DatabaseInitializer.InitializeAsync(provider);
        (await check.CheckHealthAsync(new HealthCheckContext())).Status.Should().Be(HealthStatus.Healthy);
    }

    [Fact]
    public async Task Parallel_runners_are_serialized_by_the_advisory_lock()
    {
        await using var provider = CreateProvider();
        var active = 0;
        var maximumActive = 0;
        async Task BeforeMigration(EnglishAiDbContext _, CancellationToken cancellationToken)
        {
            var current = Interlocked.Increment(ref active);
            maximumActive = Math.Max(maximumActive, current);
            await Task.Delay(350, cancellationToken);
            Interlocked.Decrement(ref active);
        }

        var options = new DatabaseInitializationOptions(BeforeMigration: BeforeMigration);
        await Task.WhenAll(
            DatabaseInitializer.InitializeAsync(provider, options),
            DatabaseInitializer.InitializeAsync(provider, options));

        maximumActive.Should().Be(1);
    }

    [Fact]
    public async Task Failed_runner_releases_the_lock_for_the_next_runner()
    {
        await using var provider = CreateProvider();
        var failing = new DatabaseInitializationOptions(
            BeforeMigration: (_, _) => throw new InvalidOperationException("synthetic migration failure"));

        await FluentActions.Invoking(() => DatabaseInitializer.InitializeAsync(provider, failing))
            .Should().ThrowAsync<InvalidOperationException>();

        var result = await DatabaseInitializer.InitializeAsync(provider);
        result.PendingMigrations.Should().NotBeEmpty();
    }

    [Fact]
    public async Task Disabled_reconciliation_does_not_remove_retired_rows()
    {
        await using var provider = CreateProvider();
        await DatabaseInitializer.InitializeAsync(provider, new DatabaseInitializationOptions(ReconcileCatalog: false));
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<EnglishAiDbContext>();
        (await db.Database.GetPendingMigrationsAsync()).Should().BeEmpty();
        (await db.VocabularyTopics.AnyAsync()).Should().BeTrue();
    }

    private ServiceProvider CreateProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContextPool<EnglishAiDbContext>(options => options.UseNpgsql(_postgres.GetConnectionString()));
        return services.BuildServiceProvider();
    }

    private DatabaseSchemaHealthCheck CreateSchemaHealthCheck(IServiceProvider provider)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:Postgres"] = _postgres.GetConnectionString()
            })
            .Build();
        return new DatabaseSchemaHealthCheck(provider, configuration);
    }
}
