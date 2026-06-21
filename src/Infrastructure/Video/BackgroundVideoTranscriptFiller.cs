using System.Collections.Concurrent;
using Application.Video.FillVideoTranscript;
using Application.Video.Ports;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// Runs transcript fills on a background <see cref="Task"/> in a fresh DI scope, so a GET on a
/// not-yet-transcribed lesson returns immediately instead of blocking on the yt-dlp subprocess
/// and LLM translation. Works whether or not Hangfire is enabled (it is off by default in dev),
/// keeping the player fast in every environment. Fills are de-duplicated per lesson id so
/// repeated opens or the client's poll don't spawn overlapping work.
/// </summary>
public sealed class BackgroundVideoTranscriptFiller : IVideoTranscriptFiller
{
    // A provider may be temporarily blocked by YouTube or briefly unavailable. Without a cooldown,
    // each 2.5-second player poll starts a fresh process/network chain as soon as the previous
    // attempt finishes. That wastes work and leaves the learner staring at a misleading spinner.
    private static readonly TimeSpan RetryCooldown = TimeSpan.FromSeconds(30);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<BackgroundVideoTranscriptFiller> _logger;
    private readonly TimeProvider _clock;
    private readonly ConcurrentDictionary<Guid, byte> _inFlight = new();
    private readonly ConcurrentDictionary<Guid, DateTimeOffset> _retryAfter = new();

    public BackgroundVideoTranscriptFiller(
        IServiceScopeFactory scopeFactory,
        ILogger<BackgroundVideoTranscriptFiller> logger,
        TimeProvider clock)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _clock = clock;
    }

    public void RequestFill(Guid videoLessonId)
    {
        var now = _clock.GetUtcNow();
        if (_retryAfter.TryGetValue(videoLessonId, out var retryAfter))
        {
            if (retryAfter > now)
            {
                _logger.LogDebug(
                    "Skipping transcript fill for lesson {LessonId}; retry is deferred until {RetryAfter}.",
                    videoLessonId, retryAfter);
                return;
            }

            _retryAfter.TryRemove(videoLessonId, out _);
        }

        // Only one fill per lesson at a time; a duplicate request while one is in flight is a no-op.
        if (!_inFlight.TryAdd(videoLessonId, 0))
            return;

        _ = Task.Run(async () =>
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var sender = scope.ServiceProvider.GetRequiredService<ISender>();
                var filled = await sender.Send(new FillVideoTranscriptCommand(videoLessonId));
                if (filled)
                {
                    _retryAfter.TryRemove(videoLessonId, out _);
                }
                else
                {
                    DeferRetry(videoLessonId);
                }
            }
            catch (Exception ex)
            {
                // Enrichment is best-effort: a failed fetch/translate must not crash anything.
                // The player keeps its honest "transcript pending" state (docs/development-guide.md rules 8, 11),
                // but client polling must not immediately fan that one failure into many retries.
                DeferRetry(videoLessonId);
                _logger.LogWarning(ex, "Background transcript fill failed for lesson {LessonId}.", videoLessonId);
            }
            finally
            {
                _inFlight.TryRemove(videoLessonId, out _);
            }
        });
    }

    private void DeferRetry(Guid videoLessonId)
    {
        var retryAfter = _clock.GetUtcNow().Add(RetryCooldown);
        _retryAfter[videoLessonId] = retryAfter;
        _logger.LogInformation(
            "Transcript fill for lesson {LessonId} produced no lines; retry deferred until {RetryAfter}.",
            videoLessonId, retryAfter);
    }
}
