using Application.Video.Models;
using Application.Video.Ports;
using Domain.Video;
using MediatR;

namespace Application.Video.FillVideoTranscript;

public sealed class FillVideoTranscriptCommandHandler : IRequestHandler<FillVideoTranscriptCommand, bool>
{
    // A transcript at or below this many lines is shown in one chunk - it appears the moment it
    // arrives, so chunking would only add round-trips for no perceived gain. Past it the transcript
    // is split so the opening lines paint first (docs/development-guide.md rule 8 - never block on the slow path).
    private const int SingleShotMaxLines = 80;

    // The first shown chunk is ~1% of the transcript from the start, with a small floor so the
    // learner sees real opening dialogue almost immediately while the remaining chunks continue
    // in the background.
    private const double FirstChunkFraction = 0.01;
    private const int MinFirstChunkLines = 8;

    // Each later chunk is shown in slices of this size; small enough that the player keeps growing,
    // large enough to keep the per-chunk translation batched.
    private const int FollowOnChunkLines = 60;

    private readonly IVideoRepository _videos;
    private readonly IVideoTranscriptProvider _transcripts;
    private readonly IVideoTranscriptTranslator _translator;

    public FillVideoTranscriptCommandHandler(
        IVideoRepository videos,
        IVideoTranscriptProvider transcripts,
        IVideoTranscriptTranslator translator)
    {
        _videos = videos;
        _transcripts = transcripts;
        _translator = translator;
    }

    public async Task<bool> Handle(FillVideoTranscriptCommand request, CancellationToken cancellationToken)
    {
        var lesson = await _videos.GetByIdAsync(request.VideoLessonId, cancellationToken);
        // The lesson may have been removed, or already filled by a concurrent request - nothing to do.
        if (lesson is null || lesson.Transcript.Count > 0)
            return false;

        // The video's own caption track (manual or auto-generated) is the only transcript source: it
        // carries the per-word timing for the karaoke highlight. There is no speech-to-text fallback -
        // a video with no captions stays without a transcript rather than getting a recognized one.
        var result = await _transcripts.FetchAsync(lesson.YouTubeVideoId, cancellationToken);

        if (result.Outcome == TranscriptFetchOutcome.NoCaptions)
        {
            // The provider ran and confirmed the video has no caption track: mark the transcript
            // terminally unavailable and persist that, so the player shows an honest "no transcript"
            // message and stops polling instead of spinning forever (docs/development-guide.md rule 8).
            lesson.MarkTranscriptUnavailable();
            await _videos.SaveAsync(lesson, cancellationToken);
            return false;
        }

        var lines = result.Lines;
        if (result.Outcome == TranscriptFetchOutcome.ProviderUnavailable || lines.Count == 0)
        {
            // The provider could not run or failed transiently (tool missing, rate-limit, network).
            // Leave the lesson Pending - never terminal - so the next open retries instead of
            // permanently poisoning an otherwise-captioned video into "no transcript" (docs/development-guide.md rule 8).
            return false;
        }

        // The slices the transcript is shown in: a short transcript is a single chunk; a long one gets
        // a small first slice (~1%) so the opening paints first, then FollowOnChunkLines-sized slices.
        var chunks = SplitIntoChunks(lines);

        // PASS 1 - paint English immediately, never waiting on the LLM. Appending a chunk is just a DB
        // write, so the whole transcript appears within moments of the captions being fetched: the
        // first ~1% lands as Partial, the rest follows, and the final chunk flips it to Available. The
        // player renders English only, so the learner reads at once instead of waiting on translation
        // (docs/development-guide.md rule 8 - never block display on the slow path).
        for (var i = 0; i < chunks.Count; i++)
        {
            var isFinal = i == chunks.Count - 1;
            lesson.AppendTranscript(ToSegments(chunks[i]), isFinal);
            await _videos.SaveAsync(lesson, cancellationToken);
        }

        // PASS 2 - enrich off the display path: translate every chunk for the per-line Uzbek text and
        // the vetted word glossary, then write the finished transcript back. This no longer gates the
        // on-screen transcript; it enriches the lesson for word lookups and future loads. Translation
        // is best-effort - on no key / failure it is empty, so the lesson stays English-only (rules 8, 11).
        await TranslateInBackgroundAsync(lesson, chunks, cancellationToken);
        return true;
    }

    /// <summary>
    /// Translates each chunk (its glossary merged into the lesson) and, if any translation came back,
    /// rewrites the transcript with the translated lines. On an empty translator (no key / failure)
    /// it leaves the English-only transcript already shown untouched (docs/development-guide.md rules 8, 11).
    /// </summary>
    private async Task TranslateInBackgroundAsync(
        VideoLesson lesson, IReadOnlyList<IReadOnlyList<TranscriptLine>> chunks, CancellationToken cancellationToken)
    {
        var translated = new List<TranscriptSegment>(lesson.Transcript.Count);
        var anyTranslation = false;

        foreach (var chunk in chunks)
        {
            var translation = await _translator.TranslateAsync(chunk, cancellationToken);
            if (translation.LineTranslations.Count > 0 || translation.Glossary.Count > 0)
                anyTranslation = true;

            for (var i = 0; i < chunk.Count; i++)
            {
                var line = chunk[i];
                translated.Add(TranscriptSegment.Create(
                    line.StartSeconds, line.EndSeconds, line.EnglishText,
                    line.UzbekTranslation ?? LineTranslationAt(translation, i),
                    line.Words));
            }

            lesson.AddGlossary(translation.Glossary.Select(g => new VideoGlossaryEntry(g.Word, g.UzbekMeaning)));
        }

        // Nothing came back: keep the English-only transcript that PASS 1 already shows and persisted.
        if (!anyTranslation)
            return;

        lesson.SetTranscript(translated);
        await _videos.SaveAsync(lesson, cancellationToken);
    }

    private static IEnumerable<TranscriptSegment> ToSegments(IReadOnlyList<TranscriptLine> lines) =>
        lines.Select(line => TranscriptSegment.Create(
            line.StartSeconds, line.EndSeconds, line.EnglishText, line.UzbekTranslation, line.Words));

    /// <summary>
    /// Splits the lines into the slices the transcript is shown in: a short transcript is one slice;
    /// a long one is a small first slice (~1% from the start, floored at <see cref="MinFirstChunkLines"/>)
    /// followed by <see cref="FollowOnChunkLines"/>-sized slices.
    /// </summary>
    private static IReadOnlyList<IReadOnlyList<TranscriptLine>> SplitIntoChunks(IReadOnlyList<TranscriptLine> lines)
    {
        if (lines.Count <= SingleShotMaxLines)
            return new[] { lines };

        var chunks = new List<IReadOnlyList<TranscriptLine>>();
        var firstChunkSize = Math.Clamp(
            (int)Math.Ceiling(lines.Count * FirstChunkFraction), MinFirstChunkLines, lines.Count);

        var index = 0;
        var size = firstChunkSize;
        while (index < lines.Count)
        {
            var take = Math.Min(size, lines.Count - index);
            chunks.Add(Slice(lines, index, take));
            index += take;
            size = FollowOnChunkLines;
        }

        return chunks;
    }

    private static IReadOnlyList<TranscriptLine> Slice(IReadOnlyList<TranscriptLine> lines, int start, int count)
    {
        var slice = new List<TranscriptLine>(count);
        for (var i = 0; i < count; i++)
            slice.Add(lines[start + i]);
        return slice;
    }

    private static string? LineTranslationAt(TranscriptTranslation translation, int index) =>
        index < translation.LineTranslations.Count ? translation.LineTranslations[index] : null;
}
