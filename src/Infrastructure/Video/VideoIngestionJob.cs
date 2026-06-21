using Application.Video.IngestVideo;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// Hangfire entry point for ingesting a video off the request path, so the quota-limited
/// YouTube + LLM calls run in the background (PROJECT-SPEC B.3, docs/development-guide.md rule 17.3). It
/// is a thin adapter: all logic lives in the <see cref="IngestVideoCommand"/> handler,
/// which is unit-tested independently.
/// </summary>
public sealed class VideoIngestionJob
{
    private readonly ISender _sender;
    private readonly ILogger<VideoIngestionJob> _logger;

    public VideoIngestionJob(ISender sender, ILogger<VideoIngestionJob> logger)
    {
        _sender = sender;
        _logger = logger;
    }

    public async Task RunAsync(string youTubeVideoId, string topic)
    {
        var lesson = await _sender.Send(new IngestVideoCommand(youTubeVideoId, topic));
        _logger.LogInformation(
            "Ingested video {VideoId} as {Level} ({Title}).",
            lesson.YouTubeVideoId, lesson.Level, lesson.Title);
    }
}
