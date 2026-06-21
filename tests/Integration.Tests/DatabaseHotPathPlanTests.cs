using System.Text.Json;
using FluentAssertions;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Testcontainers.PostgreSql;
using Xunit;

namespace Integration.Tests;

[Trait("Category", "Integration")]
public sealed class DatabaseHotPathPlanTests : IAsyncLifetime
{
    private const int LearnerCount = 100_000;
    private static readonly Guid TargetLearner = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        .WithImage("postgres:16-alpine")
        .Build();

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();
        await using var context = new EnglishAiDbContext(Options());
        await context.Database.MigrateAsync();
        await SeedSyntheticScaleDataAsync(context);
    }

    public Task DisposeAsync() => _postgres.DisposeAsync().AsTask();

    private DbContextOptions<EnglishAiDbContext> Options() =>
        new DbContextOptionsBuilder<EnglishAiDbContext>()
            .UseNpgsql(_postgres.GetConnectionString(), options => options.CommandTimeout(180))
            .Options;

    [Fact]
    public async Task Hundred_thousand_user_hot_paths_use_the_evidence_backed_indexes()
    {
        await using var connection = new NpgsqlConnection(_postgres.GetConnectionString());
        await connection.OpenAsync();

        var vocabularyPlan = await ExplainAsync(connection,
            """
            SELECT "Id", "Word", "Schedule_NextReviewAt"
            FROM "VocabularyItems"
            WHERE "LearnerId" = @learnerId
              AND "Schedule_Stage" <> 3
              AND "Schedule_NextReviewAt" IS NOT NULL
              AND "Schedule_NextReviewAt" <= @now
            ORDER BY "Schedule_NextReviewAt"
            """,
            new NpgsqlParameter("learnerId", TargetLearner),
            new NpgsqlParameter("now", new DateTimeOffset(2026, 7, 30, 0, 0, 0, TimeSpan.Zero)));

        var studyPlan = await ExplainAsync(connection,
            """
            SELECT "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds",
                   "WritingSeconds", "GrammarSeconds", "VocabularySeconds"
            FROM "DailyStudyRecords"
            WHERE "LearnerId" = @learnerId AND "Day" >= DATE '2025-07-31'
            ORDER BY "Day"
            """,
            new NpgsqlParameter("learnerId", TargetLearner));

        var videoPlan = await ExplainAsync(connection,
            """
            SELECT "Id", "Level", "CreatedAt"
            FROM "VideoLessons"
            WHERE "Status" = 1 AND "Level" BETWEEN 2 AND 4
            ORDER BY "Level", "CreatedAt" DESC
            LIMIT 100
            """);

        vocabularyPlan.Should().Contain("IX_VocabularyItems_LearnerId_Schedule_NextReviewAt");
        studyPlan.Should().Contain("IX_DailyStudyRecords_LearnerId_Day");
        videoPlan.Should().Contain("IX_VideoLessons_Status_Level_CreatedAt");
    }

    private static async Task<string> ExplainAsync(
        NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters)
    {
        await using var command = connection.CreateCommand();
        command.CommandText = $"EXPLAIN (ANALYZE, BUFFERS, FORMAT JSON) {sql}";
        command.Parameters.AddRange(parameters);
        var json = (string)(await command.ExecuteScalarAsync())!;
        using var document = JsonDocument.Parse(json);
        return document.RootElement.ToString();
    }

    private static async Task SeedSyntheticScaleDataAsync(EnglishAiDbContext context)
    {
        await context.Database.ExecuteSqlRawAsync(
            $$"""
            INSERT INTO "UserAccounts" ("Id", "GoogleSubject", "Email", "DisplayName", "CreatedAt", "LastLoginAt", "ProTrialExpiresAt", "IsAdmin")
            SELECT ('00000000-0000-0000-0000-' || lpad(gs::text, 12, '0'))::uuid,
                   'scale-' || gs, 'scale-' || gs || '@example.test', 'Scale User ' || gs,
                   timestamptz '2026-01-01 00:00:00+00', timestamptz '2026-01-01 00:00:00+00',
                   timestamptz '2026-02-01 00:00:00+00', false
            FROM generate_series(1, {{LearnerCount}}) gs;

            INSERT INTO "DailyStudyRecords" ("Id", "LearnerId", "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds", "WritingSeconds", "GrammarSeconds", "VocabularySeconds")
            SELECT gen_random_uuid(), ('00000000-0000-0000-0000-' || lpad(learner::text, 12, '0'))::uuid,
                   DATE '2025-07-31' + day_offset, 30, 30, 30, 30, 30, 30
            FROM generate_series(1, {{LearnerCount}}) learner
            CROSS JOIN generate_series(0, 9) day_offset;

            INSERT INTO "VocabularyItems" ("Id", "LearnerId", "Word", "Translation", "Source", "PartOfSpeech", "CreatedAt",
                "Schedule_Id", "Schedule_LearnedAt", "Schedule_NextReviewAt", "Schedule_Stage", "Schedule_FailCount")
            SELECT gen_random_uuid(), ('00000000-0000-0000-0000-' || lpad(learner::text, 12, '0'))::uuid,
                   'word-' || item, 'translation-' || item, 0, 0, timestamptz '2026-07-01 00:00:00+00',
                   gen_random_uuid(), timestamptz '2026-07-01 00:00:00+00', timestamptz '2026-07-20 00:00:00+00', 0, 0
            FROM generate_series(1, {{LearnerCount}}) learner
            CROSS JOIN generate_series(1, 10) item;

            INSERT INTO "VideoLessons" ("Id", "YouTubeVideoId", "Title", "Channel", "DurationSeconds", "Topic", "Level", "Status", "TranscriptStatus", "CreatedAt", "Glossary")
            SELECT gen_random_uuid(), lpad(gs::text, 11, '0'), 'Scale Video ' || gs, 'Scale Channel', 300,
                   'listening', gs % 6, 1, 0, timestamptz '2026-01-01 00:00:00+00' + gs * interval '1 second', '[]'::jsonb
            FROM generate_series(1, 100000) gs;

            ANALYZE "DailyStudyRecords";
            ANALYZE "VocabularyItems";
            ANALYZE "VideoLessons";
            """);
    }
}
