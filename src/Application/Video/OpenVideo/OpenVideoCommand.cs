using Application.Video.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Video.OpenVideo;

/// <summary>
/// Opens a video discovered in the feed (PROJECT-SPEC B.3, Bosqich 2). If the video is
/// already a stored lesson it is returned as-is; otherwise it is persisted as a playable
/// lesson with the real, search-derived metadata and an empty transcript/quiz - those are
/// filled from the video's real captions later (Bosqich 3), never fabricated (docs/development-guide.md rules 8, 11).
/// </summary>
public sealed record OpenVideoCommand(
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    string Topic,
    CefrLevel Level) : IRequest<VideoLessonDto>;
