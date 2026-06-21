using MediatR;

namespace Application.Video.FillVideoTranscript;

/// <summary>
/// Best-effort enrichment that fills a lesson's interactive transcript from the video's real
/// captions and translates it (PROJECT-SPEC B.3, Bosqich 3). Runs off the request path (see
/// <see cref="Application.Video.Ports.IVideoTranscriptFiller"/>) so opening a video stays fast.
/// Returns <c>true</c> when captions were found and persisted; <c>false</c> when there are none
/// (the lesson keeps its honest "pending" state - docs/development-guide.md rules 8, 11).
/// </summary>
public sealed record FillVideoTranscriptCommand(Guid VideoLessonId) : IRequest<bool>;
