using Domain.Assessment;
using Domain.Video;

namespace Infrastructure.Video;

/// <summary>
/// Seed of the curated video catalog (PROJECT-SPEC B.3, Bosqich 1). Every entry references a
/// real, verified-live, embeddable English-learning video by id (BBC Learning English, TED-Ed,
/// VOA, …) - the file is never stored, only embedded (docs/development-guide.md rule 17.3). Liveness and
/// embeddability of each id were verified against the YouTube oEmbed endpoint; durations are
/// the videos' real <c>lengthSeconds</c>.
///
/// The timed transcript and comprehension quiz are intentionally left empty here: authoring
/// fake transcript/quiz text for a real video would mislead the learner (docs/development-guide.md rules 8, 11).
/// They are populated from the video's real captions in the transcript phase (Faza 4, Bosqich 3
/// - timedtext ingestion). Until then the native YouTube player captions remain available and
/// the lesson is fully playable.
/// </summary>
public static class VideoCatalogSeed
{
    /// <summary>The reference instant used to stamp seeded lessons.</summary>
    private static readonly DateTimeOffset SeededAt = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    /// <summary>
    /// YouTube ids of earlier seed entries that were placeholder/dead videos (404). The
    /// initializer purges any of these still present so existing dev databases self-heal to
    /// the verified-live catalog below.
    /// </summary>
    public static readonly IReadOnlySet<string> RetiredYouTubeIds = new HashSet<string>
    {
        "rGqiuhnLQYU", "8PuagpuZjps", "n9Yfqj2vXkM", "Q3oItpVa9fs", "7m1Mko7Cn5g",
        // Went dead ("This video is not available") - verified 2026-06-27 while baking transcripts.
        "8irSFvoyLHQ",
    };

    public static IReadOnlyList<VideoLesson> Lessons() => new List<VideoLesson>
    {
        // ── A1 - beginners ──────────────────────────────────────────────────────────
        Lesson("erjMgola4fQ", "A1 English Listening Practice", "Listening Time", 223, "everyday", CefrLevel.A1),
        Lesson("2B_LCxxSAGA", "Restaurant Vocabulary - Slow & Easy English", "English Listening for Beginners", 1150, "food", CefrLevel.A1),

        // ── A2 - elementary ─────────────────────────────────────────────────────────
        Lesson("gOMypAhVaXE", "A2 English Listening Practice - Travel", "Listening Time", 212, "travel", CefrLevel.A2),
        Lesson("I_tRSrPru94", "How to Introduce Yourself - Easy English Conversations", "BBC Learning English", 157, "everyday", CefrLevel.A2),
        Lesson("bq6GBbh3uhU", "Your Daily Routine - Easy English Conversations", "BBC Learning English", 297, "everyday", CefrLevel.A2),
        Lesson("4C4wlOAscvY", "Talking About Food - Real Easy English", "BBC Learning English", 343, "food", CefrLevel.A2),
        Lesson("Fxi4uYmCrPo", "Talking About Public Transport - Real Easy English", "BBC Learning English", 338, "travel", CefrLevel.A2),

        // ── B1 - intermediate (6 Minute English) ────────────────────────────────────
        Lesson("7F1iJZr-p4E", "Are You Drinking Enough Water?", "BBC Learning English", 374, "health", CefrLevel.B1),
        Lesson("C830bmzNjDc", "Is Breakfast the Most Important Meal?", "BBC Learning English", 372, "food", CefrLevel.B1),
        Lesson("2lxvNnGkdTo", "What's Your Favourite Snack?", "BBC Learning English", 375, "food", CefrLevel.B1),
        Lesson("26PrgjTboVQ", "Are You Following Your Dreams?", "BBC Learning English", 379, "everyday", CefrLevel.B1),
        Lesson("h_pvijqmolQ", "Why Read Books, Not Screens?", "BBC Learning English", 382, "books", CefrLevel.B1),

        // ── B2 - upper-intermediate ─────────────────────────────────────────────────
        Lesson("Cq8v437OWyU", "The Power of Poetry", "BBC Learning English", 373, "culture", CefrLevel.B2),
        Lesson("DsQMLrPdLf8", "Why Sitting Is Bad for Health", "BBC Learning English", 382, "health", CefrLevel.B2),
    };

    /// <summary>
    /// Builds a leveled, playable catalog lesson. Its interactive transcript is shipped pre-fetched
    /// when a baked English caption track exists for the id (<see cref="SeedTranscriptStore"/>), so the
    /// lesson loads with a working transcript even on a server whose IP YouTube blocks; otherwise it
    /// starts empty and fills lazily (see class remarks). The comprehension quiz is still filled later.
    /// </summary>
    private static VideoLesson Lesson(
        string youTubeVideoId, string title, string channel, int durationSeconds, string topic, CefrLevel level) =>
        VideoLesson.Curate(
            youTubeVideoId, title, channel, durationSeconds, topic, level,
            SeedTranscriptStore.Load(youTubeVideoId),
            Array.Empty<ComprehensionQuestion>(), SeededAt);
}
