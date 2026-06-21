using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Vocabulary;

public sealed class InMemoryVocabularyStatsReader : IVocabularyStatsReader
{
    private readonly IVocabularyRepository _repository;

    public InMemoryVocabularyStatsReader(IVocabularyRepository repository) => _repository = repository;

    public async Task<VocabularyStats> ReadAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var items = await _repository.GetByLearnerIdAsync(learnerId, cancellationToken);
        return VocabularyStatsCalculator.Compute(items, now);
    }
}

/// <summary>
/// Reads only the scalar columns needed for the vocabulary summary. Projecting the owned
/// <see cref="ReviewSchedule"/> inside grouped aggregate predicates is not translated by EF Core's
/// Npgsql provider, so the small scalar projection is materialized before the counts are computed.
/// </summary>
public sealed class EfVocabularyStatsReader : IVocabularyStatsReader
{
    private readonly EnglishAiDbContext _db;

    public EfVocabularyStatsReader(EnglishAiDbContext db) => _db = db;

    public async Task<VocabularyStats> ReadAsync(
        Guid learnerId, DateTimeOffset now, CancellationToken cancellationToken)
    {
        var connection = _db.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;
        if (shouldClose)
            await connection.OpenAsync(cancellationToken);

        try
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                """
                SELECT COUNT(*)::int,
                       COUNT(*) FILTER (WHERE "Schedule_Stage" <> 3)::int,
                       COUNT(*) FILTER (WHERE "Schedule_Stage" = 3)::int,
                       COUNT(*) FILTER (WHERE "Schedule_Stage" <> 3 AND "Schedule_NextReviewAt" IS NOT NULL AND "Schedule_NextReviewAt" <= @now)::int,
                       COUNT(*) FILTER (WHERE "CreatedAt" >= @last7)::int,
                       COUNT(*) FILTER (WHERE "CreatedAt" >= @last30)::int,
                       COALESCE(SUM("Schedule_FailCount"), 0)::int,
                       COUNT(*) FILTER (WHERE "Schedule_Stage" = 0)::int,
                       COUNT(*) FILTER (WHERE "Schedule_Stage" = 1)::int,
                       COUNT(*) FILTER (WHERE "Schedule_Stage" = 2)::int
                FROM "VocabularyItems"
                WHERE "LearnerId" = @learnerId
                """;

            AddParameter(command, "learnerId", learnerId);
            AddParameter(command, "now", now);
            AddParameter(command, "last7", now.AddDays(-7));
            AddParameter(command, "last30", now.AddDays(-30));

            await using var reader = await command.ExecuteReaderAsync(cancellationToken);
            await reader.ReadAsync(cancellationToken);

            var total = reader.GetInt32(0);
            var learning = reader.GetInt32(1);
            var mastered = reader.GetInt32(2);
            var due = reader.GetInt32(3);
            var addedLast7Days = reader.GetInt32(4);
            var addedLast30Days = reader.GetInt32(5);
            var totalFailCount = reader.GetInt32(6);

            var stages = new Dictionary<ReviewStage, int>
            {
                [ReviewStage.Day3] = reader.GetInt32(7),
                [ReviewStage.Day7] = reader.GetInt32(8),
                [ReviewStage.Day21] = reader.GetInt32(9),
                [ReviewStage.Mastered] = mastered,
            };

            return new VocabularyStats(
                total, learning, mastered, due, addedLast7Days, addedLast30Days, totalFailCount, stages);
        }
        finally
        {
            if (shouldClose)
                await connection.CloseAsync();
        }
    }

    private static void AddParameter(System.Data.Common.DbCommand command, string name, object value)
    {
        var parameter = command.CreateParameter();
        parameter.ParameterName = name;
        parameter.Value = value;
        command.Parameters.Add(parameter);
    }
}
