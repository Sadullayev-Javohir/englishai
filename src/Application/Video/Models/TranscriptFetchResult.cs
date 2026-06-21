namespace Application.Video.Models;

/// <summary>
/// How an <see cref="Ports.IVideoTranscriptProvider"/> fetch ended. It separates the two
/// "no lines" cases the fill flow must treat very differently: a video that genuinely has no
/// caption track (terminal - settle on "no transcript"), versus a provider that could not run or
/// failed transiently (tool missing, rate-limit, network, bot-check). The latter must never be
/// made terminal, or a single hiccup would permanently poison an otherwise-captioned lesson so it
/// shows "no transcript" forever and never retries (docs/development-guide.md rule 8).
/// </summary>
public enum TranscriptFetchOutcome
{
    /// <summary>Real transcript lines were retrieved.</summary>
    Fetched,

    /// <summary>The provider ran successfully and the video has no caption track - terminal.</summary>
    NoCaptions,

    /// <summary>The provider could not run or failed transiently - NOT terminal; retry on a later open.</summary>
    ProviderUnavailable,
}

/// <summary>
/// The result of a transcript fetch: the <see cref="Outcome"/> plus the <see cref="Lines"/>
/// (non-empty only for <see cref="TranscriptFetchOutcome.Fetched"/>).
/// </summary>
public sealed record TranscriptFetchResult(
    TranscriptFetchOutcome Outcome,
    IReadOnlyList<TranscriptLine> Lines)
{
    public static TranscriptFetchResult Fetched(IReadOnlyList<TranscriptLine> lines) =>
        new(TranscriptFetchOutcome.Fetched, lines);

    public static readonly TranscriptFetchResult NoCaptions =
        new(TranscriptFetchOutcome.NoCaptions, Array.Empty<TranscriptLine>());

    public static readonly TranscriptFetchResult ProviderUnavailable =
        new(TranscriptFetchOutcome.ProviderUnavailable, Array.Empty<TranscriptLine>());
}
