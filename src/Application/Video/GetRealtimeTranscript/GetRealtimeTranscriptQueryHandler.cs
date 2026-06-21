using Application.Video.Dtos;
using Application.Video.Models;
using Application.Video.Ports;
using Domain.Video;
using MediatR;

namespace Application.Video.GetRealtimeTranscript;

/// <summary>
/// Handles <see cref="GetRealtimeTranscriptQuery"/> by calling the transcript orchestrator live and
/// mapping its outcome to a status the player can act on - WITHOUT touching the repository, so nothing
/// is persisted (docs/development-guide.md real-time rule). The status mirrors <see cref="TranscriptFetchOutcome"/>:
/// "available" (lines returned), "unavailable" (the video genuinely has no captions - terminal), or
/// "pending" (every source failed transiently - the client may retry). The returned status is a machine
/// code; the Uzbek message shown to the learner is chosen from a vetted template on the client (rule 11).
/// English-only by design: no per-request LLM translation (rule 10) - words are translated on demand via
/// the existing on-click translation service.
/// </summary>
public sealed class GetRealtimeTranscriptQueryHandler
    : IRequestHandler<GetRealtimeTranscriptQuery, RealtimeTranscriptDto>
{
    private readonly IVideoTranscriptProvider _transcripts;

    public GetRealtimeTranscriptQueryHandler(IVideoTranscriptProvider transcripts) => _transcripts = transcripts;

    public async Task<RealtimeTranscriptDto> Handle(
        GetRealtimeTranscriptQuery request, CancellationToken cancellationToken)
    {
        var result = await _transcripts.FetchAsync(request.YouTubeVideoId, cancellationToken);

        return result.Outcome switch
        {
            TranscriptFetchOutcome.Fetched => new RealtimeTranscriptDto(
                RealtimeTranscriptStatus.Available, ToSegments(result.Lines)),
            TranscriptFetchOutcome.NoCaptions => new RealtimeTranscriptDto(
                RealtimeTranscriptStatus.Unavailable, Array.Empty<TranscriptSegmentDto>()),
            _ => new RealtimeTranscriptDto(
                RealtimeTranscriptStatus.Pending, Array.Empty<TranscriptSegmentDto>()),
        };
    }

    private static IReadOnlyList<TranscriptSegmentDto> ToSegments(IReadOnlyList<TranscriptLine> lines) =>
        lines.Select(line => new TranscriptSegmentDto(
            line.StartSeconds,
            line.EndSeconds,
            line.EnglishText,
            line.UzbekTranslation,
            (line.Words ?? Array.Empty<TranscriptWord>())
                .OrderBy(w => w.StartSeconds)
                .Select(TranscriptWordDto.FromDomain)
                .ToList())).ToList();
}
