using FluentAssertions;
using Infrastructure.Persistence;
using Npgsql;

namespace Integration.Tests;

public sealed class PostgresHaContractTests
{
    [Fact]
    public void Connection_budget_preserves_sixty_server_connections_of_headroom()
    {
        var applicationHosts = 3 * PostgresConnectionPolicy.PerHostServerConnectionBudget;
        var workerHosts = 2 * PostgresConnectionPolicy.PerHostServerConnectionBudget;
        var migration = 4;
        var operations = 16;

        (applicationHosts + workerHosts + migration + operations).Should().Be(PostgresConnectionPolicy.ClientConnectionBudget);
        (PostgresConnectionPolicy.GlobalMaxConnections - PostgresConnectionPolicy.ClientConnectionBudget).Should().Be(60);
    }

    [Fact]
    public void Transaction_pool_requires_port_6432_without_npgsql_pooling_or_auto_prepare()
    {
        var options = new PostgresOptions { PoolSize = 24, ServerConnectionBudget = 24, RequireTransactionPooling = true };

        var action = () => PostgresConnectionPolicy.ValidateApplication(
            "Host=127.0.0.1;Port=6432;Database=englishai;Username=app;Password=test;Pooling=false;Max Auto Prepare=0",
            options);

        action.Should().NotThrow();
    }

    [Fact]
    public void Pool_budget_sets_real_npgsql_limits()
    {
        var connectionString = PostgresConnectionPolicy.ApplyPoolBudget(
            "Host=postgres;Port=5432;Database=englishai;Username=app;Password=test;Maximum Pool Size=100",
            8);

        var builder = new NpgsqlConnectionStringBuilder(connectionString);
        builder.Pooling.Should().BeTrue();
        builder.MinPoolSize.Should().Be(0);
        builder.MaxPoolSize.Should().Be(8);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(100)]
    public void Unsafe_pool_budget_is_rejected(int requested)
    {
        var action = () => PostgresConnectionPolicy.ApplyPoolBudget(
            "Host=postgres;Port=5432;Database=englishai", requested);

        action.Should().Throw<InvalidOperationException>().WithMessage("*between 1 and 24*");
    }

    [Theory]
    [InlineData("Host=127.0.0.1;Port=5432;Database=englishai;Pooling=false;Max Auto Prepare=0")]
    [InlineData("Host=127.0.0.1;Port=6432;Database=englishai;Pooling=true;Max Auto Prepare=0")]
    [InlineData("Host=127.0.0.1;Port=6432;Database=englishai;Pooling=false;Max Auto Prepare=10")]
    public void Unsafe_transaction_pool_settings_are_rejected(string connectionString)
    {
        var options = new PostgresOptions { PoolSize = 24, ServerConnectionBudget = 24, RequireTransactionPooling = true };

        var action = () => PostgresConnectionPolicy.ValidateApplication(connectionString, options);

        action.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Hangfire_requires_session_pool_endpoint()
    {
        var action = () => PostgresConnectionPolicy.ValidateSession(
            "Host=127.0.0.1;Port=6432;Database=englishai",
            "Hangfire");

        action.Should().Throw<InvalidOperationException>().WithMessage("*6433*");
    }

    [Fact]
    public void Migration_requires_direct_read_write_primary_route()
    {
        var valid = () => PostgresConnectionPolicy.ValidateMigration(
            "Host=127.0.0.1;Port=15432;Database=englishai");
        var pooled = () => PostgresConnectionPolicy.ValidateMigration(
            "Host=127.0.0.1;Port=6432;Database=englishai;Target Session Attributes=read-write");

        valid.Should().NotThrow();
        pooled.Should().Throw<InvalidOperationException>().WithMessage("*bypass PgBouncer*");
    }

    [Fact]
    public void Single_host_migration_rejects_target_session_attributes()
    {
        var action = () => PostgresConnectionPolicy.ValidateMigration(
            "Host=postgres;Port=5432;Database=englishai;Target Session Attributes=read-write");

        action.Should().Throw<InvalidOperationException>().WithMessage("*single-host*");
    }

    [Fact]
    public void Multi_host_migration_requires_read_write_target_session()
    {
        var action = () => PostgresConnectionPolicy.ValidateMigration(
            "Host=postgres-1,postgres-2;Port=5432;Database=englishai");

        action.Should().Throw<InvalidOperationException>().WithMessage("*Target Session Attributes=read-write*");
    }

    [Fact]
    public void Multi_host_migration_accepts_read_write_target_session()
    {
        var action = () => PostgresConnectionPolicy.ValidateMigration(
            "Host=postgres-1,postgres-2;Port=5432;Database=englishai;Target Session Attributes=read-write");

        action.Should().NotThrow();
    }
}
