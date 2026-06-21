using Application.Analytics.Ports;
using Domain.Analytics;
using Domain.Learning;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Analytics;

/// <summary>
/// EF Core / PostgreSQL adapter for <see cref="IStudyLogStore"/>. One durable row per
/// (learner, day); study time accumulates there so the dashboard's year/all-time totals survive
/// restarts (unlike the Redis gamification counters).
/// </summary>
public sealed class EfStudyLogStore : IStudyLogStore
{
    private readonly EnglishAiDbContext _db;

    public EfStudyLogStore(EnglishAiDbContext db)
    {
        _db = db;
    }

    public async Task AddStudyTimeAsync(
        Guid learnerId, DateOnly day, SkillType skill, int seconds, CancellationToken cancellationToken)
    {
        // Defensively apply the domain's per-heartbeat cap (the command validator already bounds
        // this to 1..MaxHeartbeatSeconds, but the store must not trust its caller blindly).
        var credited = Math.Clamp(seconds, 0, DailyStudyRecord.MaxHeartbeatSeconds);
        if (credited == 0)
            return;

        // One durable row per (learner, day), accumulated with a single atomic upsert. The active
        // learning page fires overlapping heartbeats, so two requests could previously both read
        // "no row yet" and race to INSERT - tripping the unique (LearnerId, Day) index (SQLSTATE
        // 23505) and 500ing the endpoint - while two concurrent updates could lose an increment via
        // read-modify-write. Postgres' INSERT ... ON CONFLICT DO UPDATE performs the create-or-add
        // in one statement the database serializes, so the duplicate-key error can't happen and no
        // second is ever lost. Each branch is a constant SQL string with the values passed as
        // interpolation holes (i.e. real query parameters), so it is injection-safe by construction.
        var id = Guid.NewGuid();
        var sql = UpsertSql(skill, id, learnerId, day, credited);
        await _db.Database.ExecuteSqlInterpolatedAsync(sql, cancellationToken);
    }

    // Returns the create-or-accumulate statement for one skill. The SQL text is a compile-time
    // constant per skill (only the target column differs); {id}/{learnerId}/{day}/{credited} are
    // parameters, never string-concatenated - so neither the EF (EF1002) nor the ReSharper
    // SQL-injection analyzers can flag it.
    private static FormattableString UpsertSql(
        SkillType skill, Guid id, Guid learnerId, DateOnly day, int credited) => skill switch
    {
        SkillType.Speaking =>
            $"""INSERT INTO "DailyStudyRecords" ("Id", "LearnerId", "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds", "WritingSeconds", "GrammarSeconds", "VocabularySeconds") VALUES ({id}, {learnerId}, {day}, {credited}, 0, 0, 0, 0, 0) ON CONFLICT ("LearnerId", "Day") DO UPDATE SET "SpeakingSeconds" = "DailyStudyRecords"."SpeakingSeconds" + {credited}""",
        SkillType.Listening =>
            $"""INSERT INTO "DailyStudyRecords" ("Id", "LearnerId", "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds", "WritingSeconds", "GrammarSeconds", "VocabularySeconds") VALUES ({id}, {learnerId}, {day}, 0, {credited}, 0, 0, 0, 0) ON CONFLICT ("LearnerId", "Day") DO UPDATE SET "ListeningSeconds" = "DailyStudyRecords"."ListeningSeconds" + {credited}""",
        SkillType.Reading =>
            $"""INSERT INTO "DailyStudyRecords" ("Id", "LearnerId", "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds", "WritingSeconds", "GrammarSeconds", "VocabularySeconds") VALUES ({id}, {learnerId}, {day}, 0, 0, {credited}, 0, 0, 0) ON CONFLICT ("LearnerId", "Day") DO UPDATE SET "ReadingSeconds" = "DailyStudyRecords"."ReadingSeconds" + {credited}""",
        SkillType.Writing =>
            $"""INSERT INTO "DailyStudyRecords" ("Id", "LearnerId", "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds", "WritingSeconds", "GrammarSeconds", "VocabularySeconds") VALUES ({id}, {learnerId}, {day}, 0, 0, 0, {credited}, 0, 0) ON CONFLICT ("LearnerId", "Day") DO UPDATE SET "WritingSeconds" = "DailyStudyRecords"."WritingSeconds" + {credited}""",
        SkillType.Grammar =>
            $"""INSERT INTO "DailyStudyRecords" ("Id", "LearnerId", "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds", "WritingSeconds", "GrammarSeconds", "VocabularySeconds") VALUES ({id}, {learnerId}, {day}, 0, 0, 0, 0, {credited}, 0) ON CONFLICT ("LearnerId", "Day") DO UPDATE SET "GrammarSeconds" = "DailyStudyRecords"."GrammarSeconds" + {credited}""",
        SkillType.Vocabulary =>
            $"""INSERT INTO "DailyStudyRecords" ("Id", "LearnerId", "Day", "SpeakingSeconds", "ListeningSeconds", "ReadingSeconds", "WritingSeconds", "GrammarSeconds", "VocabularySeconds") VALUES ({id}, {learnerId}, {day}, 0, 0, 0, 0, 0, {credited}) ON CONFLICT ("LearnerId", "Day") DO UPDATE SET "VocabularySeconds" = "DailyStudyRecords"."VocabularySeconds" + {credited}""",
        _ => throw new Domain.Common.DomainException($"Unknown skill: {skill}."),
    };
    public async Task<StudyStats> GetStatsAsync(
        Guid learnerId, DateOnly today, CancellationToken cancellationToken)
    {
        var allTime = await _db.DailyStudyRecords
            .AsNoTracking()
            .Where(record => record.LearnerId == learnerId)
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalSeconds = group.Sum(record =>
                    record.SpeakingSeconds + record.ListeningSeconds + record.ReadingSeconds +
                    record.WritingSeconds + record.GrammarSeconds + record.VocabularySeconds),
                ActiveDays = group.Count(record =>
                    record.SpeakingSeconds + record.ListeningSeconds + record.ReadingSeconds +
                    record.WritingSeconds + record.GrammarSeconds + record.VocabularySeconds > 0),
                LongestDaySeconds = group.Max(record =>
                    record.SpeakingSeconds + record.ListeningSeconds + record.ReadingSeconds +
                    record.WritingSeconds + record.GrammarSeconds + record.VocabularySeconds),
                SpeakingSeconds = group.Sum(record => record.SpeakingSeconds),
                ListeningSeconds = group.Sum(record => record.ListeningSeconds),
                ReadingSeconds = group.Sum(record => record.ReadingSeconds),
                WritingSeconds = group.Sum(record => record.WritingSeconds),
                GrammarSeconds = group.Sum(record => record.GrammarSeconds),
                VocabularySeconds = group.Sum(record => record.VocabularySeconds),
                YearSeconds = group.Where(record => record.Day >= new DateOnly(today.Year, 1, 1) && record.Day <= today)
                    .Sum(record => record.SpeakingSeconds + record.ListeningSeconds + record.ReadingSeconds +
                                   record.WritingSeconds + record.GrammarSeconds + record.VocabularySeconds),
            })
            .SingleOrDefaultAsync(cancellationToken);

        var historyStart = today.AddDays(-364);
        var recent = await _db.DailyStudyRecords
            .AsNoTracking()
            .Where(record => record.LearnerId == learnerId && record.Day >= historyStart && record.Day <= today)
            .OrderBy(record => record.Day)
            .ToListAsync(cancellationToken);

        var bounded = StudyStatsCalculator.Compute(recent, today);
        if (allTime is null)
            return bounded;

        return bounded with
        {
            TotalSeconds = allTime.TotalSeconds,
            YearSeconds = allTime.YearSeconds,
            ActiveDays = allTime.ActiveDays,
            AverageSecondsPerActiveDay = allTime.ActiveDays == 0 ? 0 : allTime.TotalSeconds / allTime.ActiveDays,
            LongestDaySeconds = allTime.LongestDaySeconds,
            BySkill =
            [
                new SkillStudyBucket(SkillType.Speaking, allTime.SpeakingSeconds),
                new SkillStudyBucket(SkillType.Listening, allTime.ListeningSeconds),
                new SkillStudyBucket(SkillType.Reading, allTime.ReadingSeconds),
                new SkillStudyBucket(SkillType.Writing, allTime.WritingSeconds),
                new SkillStudyBucket(SkillType.Grammar, allTime.GrammarSeconds),
                new SkillStudyBucket(SkillType.Vocabulary, allTime.VocabularySeconds),
            ],
        };
    }

    public async Task<IReadOnlyList<StudyActivityDay>> GetActivityDaysAsync(
        DateOnly fromDay, CancellationToken cancellationToken)
    {
        // Project to (learner, day) DB-side so the founder dashboard never materializes full
        // per-skill rows. The (LearnerId, Day) index covers the range scan.
        var rows = await _db.DailyStudyRecords
            .AsNoTracking()
            .Where(r => r.Day >= fromDay)
            .Select(r => new { r.LearnerId, r.Day })
            .ToListAsync(cancellationToken);

        return rows.Select(r => new StudyActivityDay(r.LearnerId, r.Day)).ToList();
    }

    public async Task<GlobalStudyTotals> GetGlobalTotalsAsync(CancellationToken cancellationToken)
    {
        var totals = await _db.DailyStudyRecords
            .AsNoTracking()
            .GroupBy(_ => 1)
            .Select(group => new
            {
                TotalSeconds = group.Sum(record =>
                    (long)record.SpeakingSeconds + record.ListeningSeconds + record.ReadingSeconds +
                    record.WritingSeconds + record.GrammarSeconds + record.VocabularySeconds),
                SpeakingSeconds = group.Sum(record => (long)record.SpeakingSeconds),
            })
            .SingleOrDefaultAsync(cancellationToken);

        return totals is null
            ? new GlobalStudyTotals(0, 0)
            : new GlobalStudyTotals(totals.TotalSeconds, totals.SpeakingSeconds);
    }
}
