using Application.Common;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Speaking;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Images;

/// <summary>
/// One run's outcome: how many topics were considered (learning-spine topics + roleplay scenarios),
/// how many already had a full gallery, and how many new images were downloaded and stored this run.
/// </summary>
public sealed record TopicImageBackfillResult(int Topics, int AlreadyHadImage, int Downloaded);

/// <summary>
/// Eagerly downloads a small gallery of licensed images per learning-spine topic and stores their
/// bytes in the database (docs/development-guide.md rule 12 - fetch licensed images once, store them, re-serve from
/// the database; never hot-link). Every skill that teaches a topic (vocabulary, reading, grammar,
/// writing, listening, video) draws from the topic's gallery, so this fills illustrations for the
/// whole app in one pass - Speaking is the only module without thumbnails. Slot 0 is the cover shown
/// on catalog cards; slots 1..N illustrate the topic in different places (passage hero, gallery).
///
/// The job is idempotent: each topic is topped up only to <see cref="TargetImagesPerTopic"/>, so it
/// can be re-run to fill only the gaps (useful because providers rate-limit - images that fail this
/// run are simply retried next run). A topic with no available image is left image-less and the UI
/// shows a placeholder (rule 12) - a missing image is never a blocking failure. It mirrors
/// <c>ContentBackfillJob</c> and is triggered the same way (admin endpoint → Hangfire, off the
/// request path).
/// </summary>
public sealed class TopicImageBackfillJob
{
    /// <summary>How many images each topic's gallery aims to hold (slot 0 = cover, 1..N = extras).</summary>
    public const int TargetImagesPerTopic = 5;

    /// <summary>
    /// How many images each roleplay scenario gets. The scenario picker card shows only a cover, so
    /// one image per scenario keeps the run light (120 scenarios) while giving every card a photo.
    /// </summary>
    public const int TargetImagesPerScenario = 1;

    /// <summary>
    /// How many images each free-talk topic gets. Like roleplay scenarios, the free-talk card shows
    /// only a cover, so one image per topic keeps the run light (120 topics) while giving every card
    /// a relevant photo.
    /// </summary>
    public const int TargetImagesPerFreeTalkTopic = 1;

    // A small pause between provider calls so a full run does not burst against the API rate limit.
    // Overridable (tests pass zero so a fake-provider run over the whole catalog stays instant).
    // Lowered from 250ms to 50ms for the prod re-backfill after the image purge - the provider
    // (Commons/Unsplash) tolerates the lighter cadence and the run finishes far faster.
    private static readonly TimeSpan DefaultThrottleDelay = TimeSpan.FromMilliseconds(50);

    // Max concurrent topic downloads within a single job run. Keeps the provider call fan-out
    // bounded (DbContext/SaveAsync is not shared across topics here, each call opens its own
    // unit of work) while still parallelising the otherwise serial foreach.
    private const int MaxConcurrentDownloads = 8;

    private readonly IVocabularyTopicRepository _topics;
    private readonly IImageService _images;
    private readonly ITopicImageStore _store;
    private readonly TimeProvider _clock;
    private readonly ILogger<TopicImageBackfillJob> _logger;
    private readonly TimeSpan _throttleDelay;

    public TopicImageBackfillJob(
        IVocabularyTopicRepository topics,
        IImageService images,
        ITopicImageStore store,
        TimeProvider clock,
        ILogger<TopicImageBackfillJob> logger,
        TimeSpan? throttleDelay = null)
    {
        _topics = topics;
        _images = images;
        _store = store;
        _clock = clock;
        _logger = logger;
        _throttleDelay = throttleDelay ?? DefaultThrottleDelay;
    }

    /// <summary>One gallery to fill: a spine topic's or a roleplay scenario's (unified by id).</summary>
    private sealed record GalleryTarget(Guid Id, string Query, int TargetImages, string Label);

    public async Task<TopicImageBackfillResult> RunAsync(CancellationToken cancellationToken = default)
    {
        // 1. Load the whole topic catalogue (the repository is keyed by level, not "all") and add the
        // roleplay scenario catalog - scenarios reuse the same image store keyed by their stable
        // deterministic ImageId, so one run illustrates the whole app.
        var targets = new List<GalleryTarget>();
        foreach (var level in Enum.GetValues<CefrLevel>())
        {
            targets.AddRange((await _topics.GetByLevelAsync(level, cancellationToken))
                .Select(t => new GalleryTarget(t.Id, BuildQuery(t), TargetImagesPerTopic, t.Title)));
        }

        targets.AddRange(RoleplayScenarioCatalog.All.Select(s =>
            new GalleryTarget(s.ImageId, BuildScenarioQuery(s), TargetImagesPerScenario, s.EnglishTitle)));

        // Free-talk topics reuse the same image store keyed by their stable deterministic ImageId, so
        // this pass also illustrates every "Erkin suhbat" card (120 topics, one cover each).
        targets.AddRange(FreeTalkTopicCatalog.All.Select(t =>
            new GalleryTarget(t.ImageId, BuildFreeTalkQuery(t), TargetImagesPerFreeTalkTopic, t.EnglishTitle)));

        // 2. Top each gallery up to its target; skip full ones (idempotent, re-runnable).
        var counts = await _store.GetImageCountsAsync(cancellationToken);
        int CountFor(Guid id) => counts.TryGetValue(id, out var c) ? c : 0;
        var fullGalleries = targets.Count(t => CountFor(t.Id) >= t.TargetImages);
        var pending = targets.Where(t => CountFor(t.Id) < t.TargetImages).ToList();

        if (pending.Count == 0)
        {
            _logger.LogInformation("Topic image backfill: nothing pending - every gallery is full.");
            return new TopicImageBackfillResult(targets.Count, fullGalleries, 0);
        }

        _logger.LogInformation(
            "Topic image backfill: {Pending} of {Total} galleries need more images.",
            pending.Count, targets.Count);

        var downloaded = 0;
        // Download is the slow part (network + provider API), so fan it out concurrently.
        // DbContext is registered scoped and is NOT thread-safe, so the actual SAVE must stay
        // sequential below - we collect downloaded bytes here, then persist one-by-one.
        using var semaphore = new SemaphoreSlim(MaxConcurrentDownloads);
        var collected = new List<(Guid Id, int Have, IReadOnlyList<DownloadedImage> Images)>();

        var downloadTasks = pending.Select(async target =>
        {
            await semaphore.WaitAsync(cancellationToken);
            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var have = CountFor(target.Id);
                var need = target.TargetImages - have;
                var images = await _images.DownloadImagesAsync(target.Query, need, cancellationToken);
                if (images.Count == 0)
                {
                    _logger.LogDebug("Topic image backfill: no images for '{Label}' this run.", target.Label);
                    return;
                }

                lock (collected)
                    collected.Add((target.Id, have, images));
            }
            finally
            {
                semaphore.Release();
            }
        }).ToArray();

        await Task.WhenAll(downloadTasks);

        // Sequential persist on the single scoped DbContext (thread-safe).
        foreach (var (id, have, images) in collected)
        {
            for (var i = 0; i < images.Count; i++)
            {
                var image = images[i];
                // Re-encode to WebP so the bytea cache stays small (rule 12 - store once,
                // but store it compactly). Already-webp or undecodable bytes pass through.
                var optimized = WebpConverter.ToWebp(image.Data, image.ContentType);
                await _store.SaveAsync(
                    new TopicImageContent(
                        id, have + i, optimized.Data, optimized.ContentType, image.Source, image.Attribution,
                        image.SourceUrl, image.Width, image.Height, _clock.GetUtcNow(),
                        SafetyStatus: image.SafetyStatus,
                        SafetyModelVersion: image.SafetyModelVersion,
                        SafetyCheckedAt: image.SafetyCheckedAt,
                        SafetyReasons: image.SafetyReasons),
                    cancellationToken);
                downloaded++;
            }

            if (_throttleDelay > TimeSpan.Zero)
                await Task.Delay(_throttleDelay, cancellationToken);
        }

        _logger.LogInformation(
            "Topic image backfill: downloaded {Downloaded} new images ({Full} galleries already full).",
            downloaded, fullGalleries);

        return new TopicImageBackfillResult(targets.Count, fullGalleries, downloaded);
    }

    // A few topics whose bare title is ambiguous or surfaces off-topic/adult results on Commons get a
    // specific, child-appropriate search phrase instead (keyed by the stable per-level slug). Examples:
    // "Drawing Pictures" matched fine-art nude studies; "After School" matched the K-pop band of that
    // name. Add a slug here whenever a topic's gallery comes back wrong. The denylist in
    // WikimediaImageService is the safety net; a good query is the real fix.
    private static readonly IReadOnlyDictionary<string, string> QueryOverrides = new Dictionary<string, string>
    {
        ["a1-drawing-pictures"] = "child crayon drawing",
        ["a2-after-school"] = "children after school activities",
        ["a2-my-first-trip"] = "family travel suitcase airport transport",
        ["a2-a-hot-summer"] = "summer sunshine thermometer landscape no people",
        ["b1-festivals-around-the-world"] = "cultural festival parade celebration",
    };

    // The image search keyword for a topic: a curated override when present, else the English title
    // (e.g. "My Family", "Climate Change").
    private static string BuildQuery(Domain.Vocabulary.VocabularyTopic topic) =>
        QueryOverrides.TryGetValue(topic.Slug, out var query) ? query : topic.Title;

    // Roleplay scenario titles whose bare form is too abstract or ambiguous for an image search get a
    // curated phrase instead (keyed by the stable scenario code); the rest search by English title
    // with leading scene prepositions ("At the …") stripped so the subject leads the query.
    private static readonly IReadOnlyDictionary<string, string> ScenarioQueryOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["shop"] = "clothing store interior",
            ["taxi"] = "taxi cab city street",
            ["lost_bag"] = "information desk shopping mall",
            ["clinic_call"] = "medical receptionist phone",
            ["talking_to_teacher"] = "teacher student classroom talking",
            ["lost_wallet"] = "police station front desk",
            ["tech_support"] = "call center support headset",
            ["friendly_debate"] = "friends talking cafe discussion",
            ["claim_dispute"] = "insurance documents phone call",
            ["giving_feedback"] = "manager employee one on one meeting",
            ["key_client_rescue"] = "business meeting handshake office",
            ["keynote_qna"] = "keynote speaker conference stage",
            ["legacy_interview"] = "interview recording microphone conversation",
        };

    private static string BuildScenarioQuery(RoleplayScenarioDefinition scenario)
    {
        if (ScenarioQueryOverrides.TryGetValue(scenario.Code, out var query))
            return query;

        var title = scenario.EnglishTitle;
        foreach (var prefix in new[] { "At the ", "At a ", "On an ", "On a ", "A ", "An " })
        {
            if (title.StartsWith(prefix, StringComparison.Ordinal) && title.Length > prefix.Length + 3)
                return title[prefix.Length..];
        }

        return title;
    }

    // Free-talk topics whose title is too abstract for a literal image search (mostly C1/C2 themes)
    // get a concrete, photographable phrase instead (keyed by the stable topic code); the rest search
    // by English title with the leading possessive/article ("My ", "The ", "A ") stripped so the
    // subject leads the query (e.g. "My family" → "family"). Abstract topics with no good override
    // simply fall back to the placeholder (rule 12) - a missing photo is never a broken card.
    private static readonly IReadOnlyDictionary<string, string> FreeTalkQueryOverrides =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["the_role_of_technology"] = "technology devices",
            ["social_media_influence"] = "social media smartphone",
            ["the_future_of_jobs"] = "modern workplace office",
            ["personal_development"] = "person reading books study",
            ["artificial_intelligence"] = "artificial intelligence robot",
            ["ethics_in_business"] = "business meeting handshake",
            ["the_economy"] = "stock market finance",
            ["mental_health_awareness"] = "calm nature wellbeing",
            ["the_media_and_truth"] = "newspaper journalism",
            ["innovation_and_society"] = "innovation laboratory",
            ["cultural_identity"] = "cultural festival traditional clothing",
            ["the_value_of_education"] = "students classroom learning",
            ["privacy_in_the_digital_age"] = "computer security lock",
            ["work_and_automation"] = "factory robot automation",
            ["social_inequality"] = "city contrast people",
            ["the_arts_in_society"] = "art gallery museum",
            ["science_and_progress"] = "science laboratory research",
            ["the_meaning_of_success"] = "mountain summit achievement",
            ["the_philosophy_of_happiness"] = "smiling people sunset",
            ["free_will_and_determinism"] = "crossroads path choice",
            ["the_ethics_of_ai"] = "robot artificial intelligence",
            ["geopolitics"] = "world map globe",
            ["the_nature_of_creativity"] = "artist painting studio",
            ["economic_globalization"] = "shipping containers port",
            ["the_future_of_humanity"] = "futuristic city skyline",
            ["language_and_thought"] = "books library reading",
            ["moral_dilemmas"] = "balance scale justice",
            ["the_role_of_government"] = "parliament government building",
            ["scientific_ethics"] = "laboratory microscope research",
            ["art_and_meaning"] = "abstract painting art",
            ["justice_and_law"] = "courtroom justice gavel",
            ["the_information_age"] = "data server technology",
            ["cultural_relativism"] = "diverse cultures people",
            ["the_psychology_of_decisions"] = "brain thinking choice",
            ["power_and_responsibility"] = "leadership team meeting",
        };

    private static string BuildFreeTalkQuery(FreeTalkTopic topic)
    {
        if (FreeTalkQueryOverrides.TryGetValue(topic.Code, out var query))
            return query;

        var title = topic.EnglishTitle;
        foreach (var prefix in new[] { "My ", "The ", "A ", "An " })
        {
            if (title.StartsWith(prefix, StringComparison.Ordinal) && title.Length > prefix.Length + 3)
                return title[prefix.Length..];
        }

        return title;
    }
}
