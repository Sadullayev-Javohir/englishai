using System.Text.Json;
using Application.Grammar.Content;
using Domain.Books;
using Domain.Curriculum;
using Domain.Grammar;
using Domain.Learning;
using Domain.Listening;
using Domain.Reading;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Curriculum;

public sealed class CurriculumBackfillService
{
    private const int MaxAttempts = 6;
    private static readonly TimeSpan Lease = TimeSpan.FromMinutes(10);
    private readonly EnglishAiDbContext _db;
    private readonly EdubaseBackfillClient _client;
    private readonly BackfillLlmOptions _options;
    private readonly ILogger<CurriculumBackfillService> _logger;

    public CurriculumBackfillService(EnglishAiDbContext db, EdubaseBackfillClient client,
        BackfillLlmOptions options, ILogger<CurriculumBackfillService> logger)
    { _db = db; _client = client; _options = options; _logger = logger; }

    public async Task<Guid> StartOrResumeAsync(bool createNew, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var run = createNew ? null : await _db.CurriculumGenerationRuns
            .Where(x => x.Version == "4" && (x.Status == CurriculumRunStatus.Running || x.Status == CurriculumRunStatus.Paused || x.Status == CurriculumRunStatus.Failed))
            .OrderByDescending(x => x.CreatedAt).FirstOrDefaultAsync(cancellationToken);
        if (run is null)
        {
            run = CurriculumGenerationRun.Start("4", "edubase", _options.ResolvedModel, now);
            _db.CurriculumGenerationRuns.Add(run);
            await _db.SaveChangesAsync(cancellationToken);
            await EnsureItemsAsync(run.Id, cancellationToken);
        }
        else { run.Resume(now); await _db.SaveChangesAsync(cancellationToken); }
        await ProcessAsync(run, cancellationToken);
        return run.Id;
    }

    public async Task<int> RunOneBatchAsync(Guid runId, int batchSize, CancellationToken cancellationToken)
    {
        var run = await _db.CurriculumGenerationRuns.FirstAsync(x => x.Id == runId, cancellationToken);
        run.Resume(DateTimeOffset.UtcNow); await _db.SaveChangesAsync(cancellationToken);
        return await ProcessBatchAsync(run, Math.Clamp(batchSize, 1, 100), cancellationToken);
    }

    public async Task PauseAsync(Guid runId, CancellationToken cancellationToken)
    {
        var run = await _db.CurriculumGenerationRuns.FirstAsync(x => x.Id == runId, cancellationToken);
        run.Pause(DateTimeOffset.UtcNow); await _db.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> ResetFailedAsync(Guid runId, CancellationToken cancellationToken)
    {
        var failed = await _db.CurriculumGenerationItems
            .Where(x => x.RunId == runId &&
                (x.Status == CurriculumItemStatus.FailedPermanent || x.Status == CurriculumItemStatus.NeedsHumanReview))
            .ToListAsync(cancellationToken);
        var now = DateTimeOffset.UtcNow;
        foreach (var item in failed) item.Reset(now);
        await _db.SaveChangesAsync(cancellationToken);
        return failed.Count;
    }

    public async Task<int> RevalidateAsync(Guid runId, CancellationToken cancellationToken)
    {
        var items = await _db.CurriculumGenerationItems
            .Where(x => x.RunId == runId && x.Status == CurriculumItemStatus.Approved && x.PayloadJson != null)
            .ToListAsync(cancellationToken);
        var reset = 0;
        var now = DateTimeOffset.UtcNow;
        foreach (var item in items)
        {
            var validation = CurriculumPayloadValidator.Validate(item.Module, item.PayloadJson!);
            if (validation.Approved) continue;
            item.Reset(now);
            reset++;
        }
        var run = await _db.CurriculumGenerationRuns.FirstAsync(x => x.Id == runId, cancellationToken);
        if (reset > 0) run.Resume(now);
        await _db.SaveChangesAsync(cancellationToken);
        return reset;
    }

    private async Task EnsureItemsAsync(Guid runId, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var topics = await _db.VocabularyTopics.OrderBy(x => x.Level).ThenBy(x => x.Sequence).ToListAsync(cancellationToken);
        foreach (var topic in topics)
            foreach (var module in new[] { "vocabulary", "grammar", "reading", "listening", "writing", "speaking" })
                _db.CurriculumGenerationItems.Add(CurriculumGenerationItem.ForTopic(runId, module,
                    $"{topic.Slug}:{module}", topic.Id, topic.Level, CurriculumPrompts.Version, now));
        var books = await _db.Books.ToListAsync(cancellationToken);
        foreach (var book in books)
            foreach (var section in book.Sections)
                _db.CurriculumGenerationItems.Add(CurriculumGenerationItem.ForBookSection(runId,
                    $"{book.Id}:{section.Id}", book.Id, section.Id, book.Level, CurriculumPrompts.Version, now));
        await _db.SaveChangesAsync(cancellationToken);
    }

    private async Task ProcessAsync(CurriculumGenerationRun run, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await _db.Entry(run).ReloadAsync(cancellationToken);
            if (run.Status == CurriculumRunStatus.Paused) return;
            var processed = await ProcessBatchAsync(run, 1, cancellationToken);
            if (processed > 0) continue;
            var pending = await _db.CurriculumGenerationItems.AnyAsync(x => x.RunId == run.Id &&
                x.Status != CurriculumItemStatus.Approved && x.Status != CurriculumItemStatus.Published &&
                x.Status != CurriculumItemStatus.FailedPermanent && x.Status != CurriculumItemStatus.NeedsHumanReview, cancellationToken);
            if (!pending) { run.Complete(DateTimeOffset.UtcNow); await _db.SaveChangesAsync(cancellationToken); }
            return;
        }
    }

    private async Task<int> ProcessBatchAsync(CurriculumGenerationRun run, int batchSize, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var readyVocabularyTopicIds = _db.CurriculumGenerationItems
            .Where(x => x.RunId == run.Id && x.Module == "vocabulary" && x.Status == CurriculumItemStatus.Approved)
            .Select(x => x.TopicId);
        var items = await _db.CurriculumGenerationItems.Where(x => x.RunId == run.Id &&
                (x.Status == CurriculumItemStatus.Pending ||
                 x.Status == CurriculumItemStatus.NeedsRetry && x.NextAttemptAt <= now ||
                 x.Status == CurriculumItemStatus.Generating && x.LeaseExpiresAt <= now) &&
                (x.Module == "vocabulary" || x.Module == "books" || readyVocabularyTopicIds.Contains(x.TopicId)))
            .OrderBy(x => x.Level).ThenBy(x => x.Module == "vocabulary" ? 0 : 1).ThenBy(x => x.SubjectKey)
            .Take(batchSize).ToListAsync(cancellationToken);
        foreach (var item in items) await ProcessItemAsync(run, item, cancellationToken);
        return items.Count;
    }

    private async Task ProcessItemAsync(CurriculumGenerationRun run, CurriculumGenerationItem item, CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow; item.Begin(Environment.MachineName, now, Lease); await _db.SaveChangesAsync(cancellationToken);
        try
        {
            var prompt = await BuildPromptAsync(item, cancellationToken);
            if (prompt is null)
            {
                item.Retry("dependency_not_ready", "dependency_not_ready", DateTimeOffset.UtcNow, TimeSpan.FromSeconds(20));
                await _db.SaveChangesAsync(cancellationToken);
                return;
            }
            var raw = await _client.CompleteAsync(prompt.Value.System, prompt.Value.User,
                $"{run.Id}:{item.SubjectKey}:{item.AttemptCount}", cancellationToken: cancellationToken);
            if (string.IsNullOrWhiteSpace(raw)) throw new InvalidOperationException("provider_empty");
            var json = ExtractJson(raw);
            var validation = CurriculumPayloadValidator.Validate(item.Module, json);
            if (!validation.Approved) throw new InvalidOperationException(string.Join(',', validation.Issues));
            item.Approve(json, validation.ToJson(), DateTimeOffset.UtcNow); await _db.SaveChangesAsync(cancellationToken);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            var permanent = item.AttemptCount >= MaxAttempts;
            item.Retry("generation_failed", ex.Message, DateTimeOffset.UtcNow,
                TimeSpan.FromSeconds(Math.Min(900, Math.Pow(2, item.AttemptCount) * 5)), permanent);
            await _db.SaveChangesAsync(cancellationToken);
            _logger.LogWarning("Curriculum item {Subject} failed attempt {Attempt}: {Error}", item.SubjectKey, item.AttemptCount, ex.Message);
        }
    }

    private async Task<(string System, string User)?> BuildPromptAsync(CurriculumGenerationItem item, CancellationToken cancellationToken)
    {
        if (item.Module == "books")
        {
            var book = await _db.Books.FirstAsync(x => x.Id == item.BookId, cancellationToken);
            var section = book.FindSection(item.SectionId!.Value)!;
            return CurriculumPrompts.Book(book.Title, book.Synopsis, section.Title, section.Order, book.SectionCount, book.Level);
        }
        var topic = await _db.VocabularyTopics.FirstAsync(x => x.Id == item.TopicId, cancellationToken);
        if (item.Module == "vocabulary") return CurriculumPrompts.Vocabulary(topic);
        var vocabularyItem = await _db.CurriculumGenerationItems.FirstOrDefaultAsync(x => x.RunId == item.RunId &&
            x.TopicId == topic.Id && x.Module == "vocabulary" && x.Status == CurriculumItemStatus.Approved, cancellationToken);
        if (vocabularyItem?.PayloadJson is null) return null;
        using var doc = JsonDocument.Parse(vocabularyItem.PayloadJson);
        var words = doc.RootElement.GetProperty("words").EnumerateArray().Select(x => x.GetProperty("w").GetString()!).ToArray();
        var levelTopics = await _db.VocabularyTopics.Where(x => x.Level == topic.Level).ToListAsync(cancellationToken);
        var review = CurriculumPrompts.ReviewFocus(topic, levelTopics);
        return item.Module switch
        {
            "grammar" => CurriculumPrompts.Grammar(topic, words, review),
            "reading" => CurriculumPrompts.Reading(topic, words, review),
            "listening" => CurriculumPrompts.Listening(topic, words, review),
            "writing" => CurriculumPrompts.Writing(topic, words, review),
            "speaking" => CurriculumPrompts.Speaking(topic, words, review),
            _ => null,
        };
    }

    private static string ExtractJson(string raw)
    {
        var start = raw.IndexOf('{'); var end = raw.LastIndexOf('}');
        if (start < 0 || end <= start) throw new InvalidOperationException("invalid_json_envelope");
        var json = raw[start..(end + 1)]; using var _ = JsonDocument.Parse(json); return json;
    }
}
