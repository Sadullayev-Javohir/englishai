using FluentAssertions;
using Infrastructure.Persistence.Migrations;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

[Trait("Category", "Integration")]
public sealed class VocabularyDay21MigrationTests : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine").Build();

    public Task InitializeAsync() => _postgres.StartAsync();
    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    [Fact]
    public async Task Migrates_only_standard_final_checkpoints_and_preserves_history()
    {
        await using var db = new EnglishAiDbContext(new DbContextOptionsBuilder<EnglishAiDbContext>().UseNpgsql(_postgres.GetConnectionString()).Options);
        db.Database.GetMigrations().Should().Contain("20260910120000_UseDay21VocabularyReviews");
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();
        await using (var setup = new NpgsqlCommand("""
            SET TIME ZONE 'America/New_York';
            CREATE TABLE "VocabularyItems" (
                "Id" integer PRIMARY KEY, "Schedule_Stage" integer,
                "Schedule_LearnedAt" timestamptz, "Schedule_NextReviewAt" timestamptz,
                "Schedule_ReviewCount" integer, "Schedule_FailCount" integer);
            INSERT INTO "VocabularyItems" VALUES
              (1,2,'2026-08-01Z','2026-08-31Z',2,0),
              (2,1,'2026-08-01Z','2026-08-08Z',1,0),
              (3,3,'2026-08-01Z','2026-11-29Z',3,0),
              (4,2,'2026-08-01Z','2026-09-17Z',4,1),
              (5,2,'2026-08-01Z','2026-08-02Z',2,0),
              (6,0,'2026-09-10Z','2026-09-13Z',2,1),
              (7,2,'2026-03-01T12:00:00Z','2026-03-31T12:00:00Z',2,0);
            """, connection)) await setup.ExecuteNonQueryAsync();
        await using var migrate = new NpgsqlCommand(UseDay21VocabularyReviews.MigrationSql, connection);
        (await migrate.ExecuteNonQueryAsync()).Should().Be(2);
        (await migrate.ExecuteNonQueryAsync()).Should().Be(0, "the data transformation is idempotent");
        await using var read = new NpgsqlCommand("""
            SELECT "Id", "Schedule_NextReviewAt", "Schedule_Stage", "Schedule_ReviewCount", "Schedule_FailCount"
            FROM "VocabularyItems" ORDER BY "Id"
            """, connection);
        await using var reader = await read.ExecuteReaderAsync();
        var expected = new[] {
            ("2026-08-22",2,2,0), ("2026-08-08",1,1,0), ("2026-11-29",3,3,0),
            ("2026-09-17",2,4,1), ("2026-08-02",2,2,0), ("2026-09-13",0,2,1), ("2026-03-22",2,2,0)
        };
        foreach (var (date, stage, count, failures) in expected)
        {
            (await reader.ReadAsync()).Should().BeTrue();
            if (reader.GetInt32(0) == 7) reader.GetDateTime(1).Hour.Should().Be(12);
            reader.GetDateTime(1).ToString("yyyy-MM-dd").Should().Be(date);
            reader.GetInt32(2).Should().Be(stage);
            reader.GetInt32(3).Should().Be(count);
            reader.GetInt32(4).Should().Be(failures);
        }
    }
}
