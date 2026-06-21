using Application.Video.Dtos;
using MediatR;

namespace Application.Video.IngestVideo;

/// <summary>
/// Ingests a YouTube video into the curated catalog: fetches its metadata/captions,
/// auto-levels it to a CEFR band with the LLM, and stores the embed + interactive layer
/// (PROJECT-SPEC B.3, Bosqich 1). Invoked from a Hangfire background job to respect the
/// YouTube API quota (docs/development-guide.md rule 17.3) - never the video file, only metadata.
/// </summary>
public sealed record IngestVideoCommand(string YouTubeVideoId, string Topic) : IRequest<VideoLessonDto>;
