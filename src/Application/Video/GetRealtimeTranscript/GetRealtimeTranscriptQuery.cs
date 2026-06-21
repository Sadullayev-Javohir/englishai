using Application.Video.Dtos;
using MediatR;

namespace Application.Video.GetRealtimeTranscript;

/// <summary>
/// Fetches a YouTube video's transcript LIVE through the transcript orchestrator and returns it
/// directly, without ever persisting it (docs/development-guide.md - transcripts for arbitrary user-pasted videos are
/// never stored in the DB/cache; every request is real-time). Used for learner-pasted videos, where
/// storing transcripts for any URL would needlessly bloat the database. The curated catalog keeps its
/// own stored transcripts via the separate fill flow.
/// </summary>
public sealed record GetRealtimeTranscriptQuery(string YouTubeVideoId) : IRequest<RealtimeTranscriptDto>;
