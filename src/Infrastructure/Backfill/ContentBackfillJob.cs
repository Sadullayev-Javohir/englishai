using Application.Backfill;
using Application.Backfill.Models;
using Application.Backfill.Ports;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Backfill;

/// <summary>
/// Eagerly fills every learning module's content for every topic and caches it in the database, so
/// users are always served from the database and the LLM is never called on the request path
/// (docs/development-guide.md rule 10 - cost control). This is the same lazy-fill the modules already do on first
/// open, just done up front and in one cheap batch instead of per user.
///
/// The job is idempotent: each backfiller skips topics whose content is already filled, so it can be
/// re-run safely to fill only the gaps. It is module-agnostic - it loops over the registered
/// <see cref="IContentBackfiller"/>s and routes batch results back by <c>CustomId</c>. It lives in
/// Infrastructure alongside the other recurring jobs (e.g. <c>VideoIngestionJob</c>).
/// </summary>
public sealed class ContentBackfillJob
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IReadOnlyList<IContentBackfiller> _backfillers;
    private readonly IContentBatchClient _batchClient;
    private readonly ILogger<ContentBackfillJob> _logger;

    public ContentBackfillJob(
        IVocabularyTopicRepository topics,
        IEnumerable<IContentBackfiller> backfillers,
        IContentBatchClient batchClient,
        ILogger<ContentBackfillJob> logger)
    {
        _topics = topics;
        _backfillers = backfillers.ToList();
        _batchClient = batchClient;
        _logger = logger;
    }

    /// <summary>
    /// The vocabulary module key. Vocabulary is the spine root: it owns the topic's target words, and
    /// the other skills reuse those words (cross-skill reinforcement). So it must be filled in a first
    /// round, before the other modules' requests are built - otherwise their <c>TARGET WORDS</c> would
    /// be empty (the topic's words are not loaded until vocabulary fills them).
    /// </summary>
    private const string VocabularyModule = "vocab";

    public async Task<ContentBackfillResult> RunAsync(CancellationToken cancellationToken = default)
    {
        var vocab = _backfillers.Where(b => b.Module == VocabularyModule).ToList();
        var rest = _backfillers.Where(b => b.Module != VocabularyModule).ToList();

        // Round 1: fill vocabulary first so each topic's target words are persisted...
        var first = await RunRoundAsync(vocab, cancellationToken);

        // ...then reload the catalogue (now carrying the filled words) so round 2's requests can list
        // them as TARGET WORDS for the other skills to reuse.
        var second = await RunRoundAsync(rest, cancellationToken);

        var total = new ContentBackfillResult(
            first.Requested + second.Requested,
            first.Completed + second.Completed,
            first.Filled + second.Filled);

        _logger.LogInformation(
            "Content backfill (2 rounds): requested {Requested}, completed {Completed}, filled {Filled}.",
            total.Requested, total.Completed, total.Filled);

        return total;
    }

    // Runs one set of backfillers over a freshly loaded copy of the catalogue. Loading the topics
    // here (rather than once up front) is what lets round 2 see the words round 1 just filled.
    private async Task<ContentBackfillResult> RunRoundAsync(
        IReadOnlyList<IContentBackfiller> backfillers, CancellationToken cancellationToken)
    {
        if (backfillers.Count == 0)
            return new ContentBackfillResult(0, 0, 0);

        // 1. Load the whole topic catalogue (the repository is keyed by level, not "all").
        var topics = new List<VocabularyTopic>();
        foreach (var level in Enum.GetValues<CefrLevel>())
            topics.AddRange(await _topics.GetByLevelAsync(level, cancellationToken));

        var topicsById = topics.ToDictionary(t => t.Id);

        // 2. Build a request per (module, pending topic), remembering how to route each reply back.
        var requests = new List<BatchContentRequest>();
        var routing = new Dictionary<string, (IContentBackfiller Backfiller, Guid TopicId)>();
        foreach (var backfiller in backfillers)
        {
            foreach (var topic in topics)
            {
                var request = await backfiller.BuildRequestAsync(topic, cancellationToken);
                if (request is null)
                    continue;

                requests.Add(request);
                routing[request.CustomId] = (backfiller, topic.Id);
            }
        }

        if (requests.Count == 0)
        {
            _logger.LogInformation("Content backfill: nothing pending for {Modules} module(s).", backfillers.Count);
            return new ContentBackfillResult(0, 0, 0);
        }

        _logger.LogInformation("Content backfill: submitting {Count} requests across {Modules} modules.",
            requests.Count, backfillers.Count);

        // 3. Run them (batch in production; the orchestration is identical either way).
        var replies = await _batchClient.RunAsync(requests, cancellationToken);

        // 4. Parse + persist each reply through its owning backfiller.
        var filled = 0;
        foreach (var (customId, text) in replies)
        {
            if (!routing.TryGetValue(customId, out var route) ||
                !topicsById.TryGetValue(route.TopicId, out var topic))
                continue;

            try
            {
                if (await route.Backfiller.ApplyResultAsync(topic, text, cancellationToken))
                    filled++;
            }
            catch (Exception ex)
            {
                // Best-effort: one bad reply must not abort the whole backfill (rules 8, 11).
                _logger.LogWarning(ex, "Content backfill: failed to apply result for {CustomId}.", customId);
            }
        }

        return new ContentBackfillResult(requests.Count, replies.Count, filled);
    }
}
