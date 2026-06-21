using System.Text.Json;
using System.Text.RegularExpressions;
using Application.Video.Models;
using Domain.Video;

namespace Infrastructure.Video;

/// <summary>
/// Pure parsing helpers for YouTube's two public transcript shapes - the
/// <c>youtubei/v1/get_transcript</c> JSON and the <c>timedtext</c> <c>fmt=json3</c> JSON - plus
/// the small bits extracted from the watch page (transcript params, caption track URL). Kept
/// I/O-free so it is unit-testable against captured-shape fixtures.
/// </summary>
internal static partial class TranscriptParser
{
    /// <summary>Minimum line length in seconds, so a zero/negative duration never breaks the domain rule.</summary>
    private const double MinLineSeconds = 0.5;

    /// <summary>Minimum highlight span of a single word, so back-to-back words never have end == start.</summary>
    private const double MinWordSeconds = 0.08;

    /// <summary>Caps how long a word stays highlighted, so a pause after it does not stretch the highlight.</summary>
    private const double MaxWordSeconds = 1.2;

    /// <summary>Highlight span assumed for the very last word (no following word to bound it).</summary>
    private const double LastWordSeconds = 0.6;

    /// <summary>A start-to-start gap larger than this (seconds) is treated as a pause that ends a sentence.</summary>
    private const double SentenceGapSeconds = 1.0;

    /// <summary>Hard cap on words per sentence, so a long unpunctuated run still breaks into readable lines.</summary>
    private const int MaxWordsPerSentence = 14;

    /// <summary>
    /// Minimum spacing, in ms, between the synthetic start times of words split out of one multi-word
    /// caption seg. Kept above the de-dup tolerance (50 ms) so two identical words in the same line are
    /// never collapsed, and so their order survives the (key-stable) start-time sort.
    /// </summary>
    private const double SplitWordMinStepMs = 51;

    /// <summary>
    /// Maximum spacing between split-out words. Held below <see cref="SentenceGapSeconds"/> (1000 ms) so
    /// the gap between two words of the SAME line is never mistaken for a between-sentence speech pause -
    /// the words of one manual-caption line are continuous speech and must stay in one sentence.
    /// </summary>
    private const double SplitWordMaxStepMs = 500;

    /// <summary>
    /// Max stored length of a single word's text - matches the <c>Text</c> column width in
    /// <see cref="VideoLessonConfiguration"/>. A pathological token (e.g. a long URL with no spaces)
    /// is truncated to this so persisting the transcript can never fail with "value too long" and
    /// silently leave the lesson stuck "pending" (docs/development-guide.md rule 8).
    /// </summary>
    private const int MaxWordLength = 60;

    /// <summary>A word with real timing, in milliseconds, before it is grouped into a sentence.</summary>
    private sealed record RawWord(string Text, double StartMs);

    [GeneratedRegex("\"getTranscriptEndpoint\":\\{\"params\":\"([^\"]+)\"")]
    private static partial Regex TranscriptParamsRegex();

    /// <summary>A YouTube caption speaker-change marker ('&gt;&gt;') with any surrounding whitespace.</summary>
    [GeneratedRegex("\\s*>+\\s*")]
    private static partial Regex SpeakerMarkerRegex();

    /// <summary>
    /// Removes YouTube's '&gt;&gt;' speaker-change markers from a caption fragment, collapsing the
    /// whitespace they leave behind. These markers are caption notation, not spoken words, so they
    /// must never reach the learner's transcript or the karaoke word stream.
    /// </summary>
    private static string StripSpeakerMarkers(string text) =>
        SpeakerMarkerRegex().Replace(text, " ").Trim();

    /// <summary>
    /// Caps a single word at <see cref="MaxWordLength"/> so it always fits its storage column. After
    /// whitespace splitting a real word is far shorter than this; only a pathological token (a long
    /// URL with no spaces) is ever truncated, which is preferable to failing the whole transcript save.
    /// </summary>
    private static string ClampWord(string word) =>
        word.Length <= MaxWordLength ? word : word[..MaxWordLength];

    [GeneratedRegex("\"captionTracks\":\\[\\{\"baseUrl\":\"([^\"]+)\"")]
    private static partial Regex CaptionBaseUrlRegex();

    /// <summary>The <c>get_transcript</c> params token from a watch page, URL-decoded for the API body.</summary>
    public static string? ExtractTranscriptParams(string watchHtml)
    {
        var match = TranscriptParamsRegex().Match(watchHtml);
        return match.Success ? Uri.UnescapeDataString(match.Groups[1].Value) : null;
    }

    /// <summary>The first caption track's <c>baseUrl</c> from a watch page, JSON-unescaped.</summary>
    public static string? ExtractCaptionBaseUrl(string watchHtml)
    {
        var match = CaptionBaseUrlRegex().Match(watchHtml);
        if (!match.Success)
            return null;

        // The URL is embedded in the page's JSON, so "&" arrives as the escape "&".
        return match.Groups[1].Value.Replace("\\u0026", "&", StringComparison.Ordinal);
    }

    /// <summary>Parses the <c>get_transcript</c> response into ordered timed lines.</summary>
    public static IReadOnlyList<TranscriptLine> ParseGetTranscript(JsonElement root)
    {
        var lines = new List<TranscriptLine>();
        CollectSegments(root, lines);
        return Order(lines);
    }

    /// <summary>
    /// Parses a <c>timedtext</c> <c>fmt=json3</c> response into ordered sentence lines, each carrying
    /// its words with real per-word timing. YouTube's json3 stores the start of every word
    /// (<c>tStartMs</c> + a seg's <c>tOffsetMs</c>), but its events cut speech into arbitrary display
    /// chunks and pad them with newline-only "rolling" events. We therefore flatten the events into a
    /// single timed word stream (dropping the newline noise) and re-group it into readable sentences,
    /// so the player shows whole sentences and can highlight the exact spoken word from real timing
    /// (PROJECT-SPEC B.3, Bosqich 3) rather than estimating it from the line span.
    /// </summary>
    public static IReadOnlyList<TranscriptLine> ParseTimedTextJson3(JsonElement root)
    {
        if (!root.TryGetProperty("events", out var events) || events.ValueKind != JsonValueKind.Array)
            return Array.Empty<TranscriptLine>();

        return GroupIntoSentences(ExtractWords(events));
    }

    /// <summary>
    /// Flattens json3 events into an ordered, de-duplicated word stream with absolute start times.
    /// Newline-only/blank segs (the auto-caption rolling effect) are dropped, and a word repeated at
    /// the same instant by an overlapping window is collapsed.
    ///
    /// Auto-generated tracks put ONE word per seg (with its own <c>tOffsetMs</c>), but manual tracks
    /// put a whole line - several words separated by spaces/newlines - in a single seg. We split every
    /// seg on whitespace so each real word becomes its own <see cref="RawWord"/> (never a 60+ char
    /// "word" that overflows the Text column and silently fails the fill), spreading a multi-word seg's
    /// words across the event's duration so they keep a sensible, monotonic karaoke order.
    /// </summary>
    private static List<RawWord> ExtractWords(JsonElement events)
    {
        var words = new List<RawWord>();
        foreach (var ev in events.EnumerateArray())
        {
            if (!ev.TryGetProperty("segs", out var segs) || segs.ValueKind != JsonValueKind.Array)
                continue;

            // tStartMs/tOffsetMs/dDurationMs are normally numbers but can arrive as strings; read
            // tolerantly so an unexpected type never throws and 500s the lesson read.
            var startMs = ReadNumber(ev, "tStartMs");
            var durationMs = ReadNumber(ev, "dDurationMs");
            foreach (var seg in segs.EnumerateArray())
            {
                var text = seg.TryGetProperty("utf8", out var u) ? u.GetString() : null;
                if (string.IsNullOrWhiteSpace(text))
                    continue; // newline-append / blank rolling-window noise carries no word

                var cleaned = StripSpeakerMarkers(text);
                if (cleaned.Length == 0)
                    continue; // a '>>' speaker marker is notation, not a spoken word - drop it

                var segStartMs = startMs + ReadNumber(seg, "tOffsetMs");
                AppendSegmentWords(cleaned, segStartMs, durationMs, words);
            }
        }

        return OrderAndDedupe(words);
    }

    /// <summary>
    /// Parses a Supadata YouTube-transcript response - its <c>content</c> array of
    /// <c>{ text, offset, duration }</c> objects, with times in <em>milliseconds</em> - into ordered
    /// sentence lines with synthetic per-word timing. Supadata gives line-level chunks (no per-word
    /// times), so we spread each chunk's words across its duration exactly the way a manual json3
    /// caption line is split, then re-group into sentences with <see cref="GroupIntoSentences"/>. The
    /// result is the same transcript shape a native YouTube fetch produces, so the player's karaoke
    /// highlight works identically regardless of which provider supplied the captions (docs/development-guide.md rule 8).
    /// </summary>
    public static IReadOnlyList<TranscriptLine> ParseSupadataTranscript(JsonElement root)
    {
        if (!root.TryGetProperty("content", out var content) || content.ValueKind != JsonValueKind.Array)
            return Array.Empty<TranscriptLine>();

        var words = new List<RawWord>();
        foreach (var seg in content.EnumerateArray())
        {
            var text = seg.TryGetProperty("text", out var t) ? t.GetString() : null;
            if (string.IsNullOrWhiteSpace(text))
                continue;

            var cleaned = StripSpeakerMarkers(text);
            if (cleaned.Length == 0)
                continue; // a '>>' speaker marker is notation, not a spoken word

            AppendSegmentWords(cleaned, ReadNumber(seg, "offset"), ReadNumber(seg, "duration"), words);
        }

        return GroupIntoSentences(OrderAndDedupe(words));
    }

    /// <summary>
    /// Splits one caption segment's text into individual timed words and appends them to
    /// <paramref name="words"/>. Splitting on any whitespace (spaces, newlines, the &amp;nbsp; YouTube
    /// inserts) turns a whole-line seg into individual words; a single-word seg yields one token. The
    /// words are spread evenly across the seg's <paramref name="durationMs"/> so a multi-word line keeps
    /// a sensible, monotonic karaoke order; each token is clamped so a pathological long token never
    /// overflows the storage column (docs/development-guide.md rule 8).
    /// </summary>
    private static void AppendSegmentWords(string cleaned, double segStartMs, double durationMs, List<RawWord> words)
    {
        var tokens = cleaned.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        var step = tokens.Length > 1 && durationMs > 0
            ? Math.Clamp(durationMs / tokens.Length, SplitWordMinStepMs, SplitWordMaxStepMs)
            : SplitWordMinStepMs;

        for (var t = 0; t < tokens.Length; t++)
            words.Add(new RawWord(ClampWord(tokens[t]), segStartMs + t * step));
    }

    /// <summary>
    /// Sorts a raw word stream by start time and drops a word re-emitted at (almost) the same instant.
    /// A STABLE sort is required: words split from one seg, or emitted at the same instant by
    /// overlapping caption windows, share a start time and must keep their original (spoken) order -
    /// an unstable sort would scramble them into nonsense lines.
    /// </summary>
    private static List<RawWord> OrderAndDedupe(List<RawWord> words)
    {
        var ordered = words.OrderBy(w => w.StartMs).ToList();

        var deduped = new List<RawWord>(ordered.Count);
        foreach (var w in ordered)
        {
            var last = deduped.Count > 0 ? deduped[^1] : null;
            if (last is not null && last.Text == w.Text && Math.Abs(last.StartMs - w.StartMs) < 50)
                continue; // same word re-emitted by an overlapping window
            deduped.Add(w);
        }

        return deduped;
    }

    /// <summary>
    /// Groups a timed word stream into sentence lines, breaking on sentence-ending punctuation,
    /// a speech pause, or a maximum word count (so unpunctuated auto-captions still split sensibly).
    /// </summary>
    private static IReadOnlyList<TranscriptLine> GroupIntoSentences(List<RawWord> words)
    {
        var lines = new List<TranscriptLine>();
        var current = new List<TranscriptWord>();

        for (var i = 0; i < words.Count; i++)
        {
            var startSeconds = Math.Max(0, words[i].StartMs / 1000.0);
            var hasNext = i + 1 < words.Count;
            var nextStartSeconds = hasNext ? words[i + 1].StartMs / 1000.0 : 0;

            // A word lasts until the next word starts, capped so a trailing pause never stretches the
            // highlight; the last word gets a fixed span as nothing follows to bound it.
            var endSeconds = hasNext
                ? Math.Min(nextStartSeconds, startSeconds + MaxWordSeconds)
                : startSeconds + LastWordSeconds;
            if (endSeconds <= startSeconds)
                endSeconds = startSeconds + MinWordSeconds;

            current.Add(TranscriptWord.Create(words[i].Text, startSeconds, endSeconds));

            var endsSentence = EndsSentence(words[i].Text);
            var pauseAhead = hasNext && nextStartSeconds - startSeconds > SentenceGapSeconds;
            if (!hasNext || endsSentence || pauseAhead || current.Count >= MaxWordsPerSentence)
            {
                lines.Add(BuildLine(current));
                current = new List<TranscriptWord>();
            }
        }

        return lines;
    }

    /// <summary>Builds a sentence line from its words: text is the words joined, span covers them all.</summary>
    private static TranscriptLine BuildLine(IReadOnlyList<TranscriptWord> words) =>
        new(words[0].StartSeconds, words[^1].EndSeconds,
            string.Join(' ', words.Select(w => w.Text)), null, words.ToList());

    /// <summary>True when a word ends a sentence (final char is '.', '?' or '!').</summary>
    private static bool EndsSentence(string word) =>
        word.Length > 0 && word[^1] is '.' or '?' or '!';

    private static void CollectSegments(JsonElement element, List<TranscriptLine> lines)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            if (element.TryGetProperty("transcriptSegmentRenderer", out var seg)
                && TryParseSegment(seg, out var line))
            {
                lines.Add(line!);
                return;
            }

            foreach (var prop in element.EnumerateObject())
                CollectSegments(prop.Value, lines);
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
                CollectSegments(item, lines);
        }
    }

    private static bool TryParseSegment(JsonElement renderer, out TranscriptLine? line)
    {
        line = null;

        var text = renderer.TryGetProperty("snippet", out var snippet) ? ReadText(snippet) : null;
        if (string.IsNullOrWhiteSpace(text))
            return false;

        var cleaned = StripSpeakerMarkers(text);
        if (cleaned.Length == 0)
            return false; // a line that is only a '>>' speaker marker carries no transcript text

        var startMs = ReadMs(renderer, "startMs");
        var endMs = ReadMs(renderer, "endMs");
        line = Line(startMs / 1000.0, endMs / 1000.0, cleaned);
        return true;
    }

    /// <summary>Reads a numeric property tolerantly, accepting either a JSON number or a numeric string.</summary>
    private static double ReadNumber(JsonElement element, string property)
    {
        if (!element.TryGetProperty(property, out var value))
            return 0;
        if (value.ValueKind == JsonValueKind.Number)
            return value.GetDouble();
        if (value.ValueKind == JsonValueKind.String && double.TryParse(value.GetString(), out var parsed))
            return parsed;
        return 0;
    }

    private static double ReadMs(JsonElement renderer, string property)
    {
        if (!renderer.TryGetProperty(property, out var el))
            return 0;

        // The renderer stores the offsets as strings ("12340").
        if (el.ValueKind == JsonValueKind.String && double.TryParse(el.GetString(), out var s))
            return s;
        if (el.ValueKind == JsonValueKind.Number)
            return el.GetDouble();
        return 0;
    }

    private static string? ReadText(JsonElement node)
    {
        if (node.ValueKind != JsonValueKind.Object)
            return null;

        if (node.TryGetProperty("simpleText", out var simple))
            return simple.GetString();

        if (node.TryGetProperty("runs", out var runs) && runs.ValueKind == JsonValueKind.Array)
            return string.Concat(runs.EnumerateArray()
                .Select(r => r.TryGetProperty("text", out var t) ? t.GetString() : null));

        return null;
    }

    /// <summary>Builds a line, guaranteeing end &gt; start so <c>TranscriptSegment.Create</c> accepts it.</summary>
    private static TranscriptLine Line(double startSeconds, double endSeconds, string text)
    {
        var start = Math.Max(0, startSeconds);
        var end = endSeconds > start ? endSeconds : start + MinLineSeconds;
        return new TranscriptLine(start, end, text);
    }

    private static IReadOnlyList<TranscriptLine> Order(List<TranscriptLine> lines) =>
        lines.OrderBy(l => l.StartSeconds).ToList();
}
