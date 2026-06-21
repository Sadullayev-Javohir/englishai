using Infrastructure.Books;
using Infrastructure.Grammar;
using Infrastructure.Listening;
using Infrastructure.Reading;
using Infrastructure.Video;
using Infrastructure.Vocabulary;
using Infrastructure.Writing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Infrastructure.Persistence;

public sealed record DatabaseInitializationOptions(
    bool ApplyMigrations = true,
    bool SeedCatalog = true,
    bool ReconcileCatalog = true,
    TimeSpan? LockTimeout = null,
    Func<EnglishAiDbContext, CancellationToken, Task>? BeforeMigration = null);

public sealed record DatabaseInitializationResult(
    IReadOnlyList<string> PendingMigrations,
    bool SeededCatalog,
    bool ReconciledCatalog);

public static class DatabaseInitializer
{
    private const long AdvisoryLockKey = 0x454E474149444231;

    public static async Task<DatabaseInitializationResult> InitializeAsync(
        IServiceProvider services,
        DatabaseInitializationOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DatabaseInitializationOptions();
        await using var scope = services.CreateAsyncScope();
        var db = scope.ServiceProvider.GetService<EnglishAiDbContext>();
        if (db is null)
            throw new InvalidOperationException("ConnectionStrings:Postgres is required for database initialization.");

        var logger = scope.ServiceProvider.GetService<ILoggerFactory>()?.CreateLogger(typeof(DatabaseInitializer));
        await db.Database.OpenConnectionAsync(cancellationToken);
        try
        {
            await AcquireAdvisoryLockAsync(db, options.LockTimeout ?? TimeSpan.FromMinutes(5), cancellationToken);
            try
            {
                var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
                if (options.BeforeMigration is not null)
                    await options.BeforeMigration(db, cancellationToken);
                if (options.ApplyMigrations && pending.Length > 0)
                    await db.Database.MigrateAsync(cancellationToken);

                var remaining = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
                if (remaining.Length > 0)
                    throw new InvalidOperationException($"Database schema is not current; {remaining.Length} migration(s) remain pending.");

                var seeded = options.SeedCatalog && await SeedCatalogAsync(db, logger, cancellationToken);
                var reconciled = options.ReconcileCatalog && await ReconcileCatalogAsync(db, logger, cancellationToken);
                return new DatabaseInitializationResult(pending, seeded, reconciled);
            }
            finally
            {
                await ReleaseAdvisoryLockAsync(db, cancellationToken);
            }
        }
        finally
        {
            await db.Database.CloseConnectionAsync();
        }
    }

    public static async Task ValidateSchemaAsync(
        EnglishAiDbContext db,
        CancellationToken cancellationToken = default)
    {
        var pending = (await db.Database.GetPendingMigrationsAsync(cancellationToken)).ToArray();
        if (pending.Length > 0)
            throw new InvalidOperationException($"Database schema is not current; {pending.Length} migration(s) remain pending.");
    }

    private static async Task AcquireAdvisoryLockAsync(
        EnglishAiDbContext db,
        TimeSpan timeout,
        CancellationToken cancellationToken)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            var connection = (NpgsqlConnection)db.Database.GetDbConnection();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT pg_try_advisory_lock(@key)";
            command.Parameters.AddWithValue("key", AdvisoryLockKey);
            if (await command.ExecuteScalarAsync(cancellationToken) is true)
                return;

            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
        }

        throw new TimeoutException($"Timed out after {timeout} waiting for the database migration lock.");
    }

    private static async Task ReleaseAdvisoryLockAsync(
        EnglishAiDbContext db,
        CancellationToken cancellationToken)
    {
        var connection = (NpgsqlConnection)db.Database.GetDbConnection();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT pg_advisory_unlock(@key)";
        command.Parameters.AddWithValue("key", AdvisoryLockKey);
        await command.ExecuteScalarAsync(cancellationToken);
    }

    private static async Task<bool> SeedCatalogAsync(
        EnglishAiDbContext db,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var changed = false;
        if (!await db.VideoLessons.AnyAsync(cancellationToken))
        {
            await db.VideoLessons.AddRangeAsync(VideoCatalogSeed.Lessons(), cancellationToken);
            logger?.LogInformation("Seeding curated video catalog into the database.");
            changed = true;
        }

        var seededBooks = BookCatalogSeed.Books();
        var existingBookIds = await db.Books.Select(book => book.Id).ToListAsync(cancellationToken);
        var missingBooks = seededBooks.Where(book => !existingBookIds.Contains(book.Id)).ToList();
        if (missingBooks.Count > 0)
        {
            await db.Books.AddRangeAsync(missingBooks, cancellationToken);
            logger?.LogInformation("Seeding {Count} books into the library.", missingBooks.Count);
            changed = true;
        }

        if (!await db.VocabularyTopics.AnyAsync(cancellationToken))
        {
            await db.VocabularyTopics.AddRangeAsync(VocabularyTopicCatalog.Topics(), cancellationToken);
            logger?.LogInformation("Seeding vocabulary topic catalog into the database.");
            changed = true;
        }

        if (!await db.ListeningExercises.AnyAsync(cancellationToken))
        {
            var listening = ListeningCatalogSeed.Exercises();
            await db.ListeningExercises.AddRangeAsync(listening, cancellationToken);
            logger?.LogInformation("Seeding curated listening catalog into the database ({Count} exercises).", listening.Count);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);

        return changed;
    }

    private static async Task<bool> ReconcileCatalogAsync(
        EnglishAiDbContext db,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var changed = false;
        var retiredIds = VideoCatalogSeed.RetiredYouTubeIds.ToArray();
        var retired = await db.VideoLessons
            .Where(lesson => retiredIds.Contains(lesson.YouTubeVideoId))
            .ToListAsync(cancellationToken);
        if (retired.Count > 0)
        {
            db.VideoLessons.RemoveRange(retired);
            logger?.LogInformation("Purged {Count} retired/dead video lessons from the catalog.", retired.Count);
            changed = true;
        }

        var bakedIds = SeedTranscriptStore.AvailableVideoIds.ToArray();
        if (bakedIds.Length > 0)
        {
            var candidates = await db.VideoLessons
                .Where(lesson => bakedIds.Contains(lesson.YouTubeVideoId))
                .ToListAsync(cancellationToken);
            var healed = 0;
            foreach (var lesson in candidates)
            {
                if (lesson.Transcript.Count > 0)
                    continue;
                var segments = SeedTranscriptStore.Load(lesson.YouTubeVideoId);
                if (segments.Count == 0)
                    continue;
                lesson.SetTranscript(segments);
                healed++;
            }

            if (healed > 0)
            {
                logger?.LogInformation("Applied baked transcripts to {Count} catalog video lesson(s).", healed);
                changed = true;
            }
        }

        var legacyReading = await db.ReadingPassages
            .Where(passage => passage.VocabularyTopicId == null)
            .ToListAsync(cancellationToken);
        if (legacyReading.Count > 0)
        {
            db.ReadingPassages.RemoveRange(legacyReading);
            logger?.LogInformation("Purged {Count} legacy stand-alone reading passages.", legacyReading.Count);
            changed = true;
        }

        changed |= await ReconcileVocabularyTopicsAsync(db, logger, cancellationToken);

        var seededListening = ListeningCatalogSeed.Exercises();
        var existingListeningCount = await db.ListeningExercises.CountAsync(cancellationToken);
        if (existingListeningCount < seededListening.Count)
        {
            if (existingListeningCount > 0)
                db.ListeningExercises.RemoveRange(await db.ListeningExercises.ToListAsync(cancellationToken));
            await db.ListeningExercises.AddRangeAsync(seededListening, cancellationToken);
            logger?.LogInformation("Replaced listening catalog with {Count} curated exercises.", seededListening.Count);
            changed = true;
        }

        var legacyGrammar = await db.GrammarLessons
            .Where(lesson => lesson.VocabularyTopicId == null)
            .ToListAsync(cancellationToken);
        if (legacyGrammar.Count > 0)
        {
            db.GrammarLessons.RemoveRange(legacyGrammar);
            logger?.LogInformation("Purged {Count} legacy curated grammar lessons.", legacyGrammar.Count);
            changed = true;
        }

        var legacyWriting = await db.WritingTasks
            .Where(task => task.VocabularyTopicId == null)
            .ToListAsync(cancellationToken);
        if (legacyWriting.Count > 0)
        {
            db.WritingTasks.RemoveRange(legacyWriting);
            logger?.LogInformation("Purged {Count} legacy curated writing tasks.", legacyWriting.Count);
            changed = true;
        }

        if (changed)
            await db.SaveChangesAsync(cancellationToken);

        return changed;
    }

    private static async Task<bool> ReconcileVocabularyTopicsAsync(
        EnglishAiDbContext db,
        ILogger? logger,
        CancellationToken cancellationToken)
    {
        var curated = VocabularyTopicCatalog.Topics();
        var curatedBySlug = curated.ToDictionary(topic => topic.Slug, StringComparer.Ordinal);
        var existing = await db.VocabularyTopics.Include(topic => topic.Words).ToListAsync(cancellationToken);
        var existingBySlug = existing.ToDictionary(topic => topic.Slug, StringComparer.Ordinal);
        var removed = existing.Where(topic => !curatedBySlug.ContainsKey(topic.Slug)).ToList();
        if (removed.Count > 0)
            db.VocabularyTopics.RemoveRange(removed);

        var added = 0;
        foreach (var topic in curated)
        {
            if (existingBySlug.TryGetValue(topic.Slug, out var current))
            {
                current.UpdateMetadata(
                    topic.Title,
                    topic.TitleUz,
                    topic.Category,
                    topic.GrammarFocusCode,
                    topic.Sequence);
            }
            else
            {
                await db.VocabularyTopics.AddAsync(topic, cancellationToken);
                added++;
            }
        }

        if (removed.Count == 0 && added == 0)
            return false;

        logger?.LogInformation(
            "Reconciled vocabulary topic catalog: +{Added} new, -{Removed} retired ({Total} total).",
            added,
            removed.Count,
            curated.Count);
        return true;
    }
}
