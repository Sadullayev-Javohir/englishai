using System.Diagnostics;
using Application.Video.Models;
using Application.Video.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// The transcript fallback manager: an <see cref="IVideoTranscriptProvider"/> that runs an ordered list
/// of inner providers and returns the first real transcript. The order is the production chain -
/// youtubei.js sidecar → yt-dlp (cookie-less) → yt-dlp (cookie-backed, with refresh-retry) → Supadata
/// API - each its own class, so adding a provider is just one more list entry (docs/development-guide.md "new provider =
/// new class"; PROJECT-SPEC B.3).
///
/// Resilience: every provider call is isolated. A provider that THROWS, hangs past the per-provider
/// timeout, or returns <see cref="TranscriptFetchOutcome.ProviderUnavailable"/> never halts the
/// chain - it is logged and the next source is tried. Outcome rule (rule 8 - never poison a captioned
/// lesson): the first provider with real lines wins; otherwise the result is terminal
/// <see cref="TranscriptFetchOutcome.NoCaptions"/> ONLY when every provider ran and agreed the video has
/// no captions; if any provider could not run / failed transiently the result is
/// <see cref="TranscriptFetchOutcome.ProviderUnavailable"/>, so the fill retries rather than settling on
/// "no transcript" because of a momentary block. Never throws (except honouring caller cancellation).
/// </summary>
public sealed class TranscriptOrchestrator : IVideoTranscriptProvider
{
    private readonly IReadOnlyList<IVideoTranscriptProvider> _providers;
    private readonly ILogger<TranscriptOrchestrator> _logger;
    private readonly TimeSpan? _perProviderTimeout;

    public TranscriptOrchestrator(
        IReadOnlyList<IVideoTranscriptProvider> providers,
        ILogger<TranscriptOrchestrator> logger,
        TimeSpan? perProviderTimeout = null)
    {
        _providers = providers;
        _logger = logger;
        _perProviderTimeout = perProviderTimeout;
    }

    public async Task<TranscriptFetchResult> FetchAsync(
        string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        var anyUnavailable = false;

        foreach (var provider in _providers)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var name = provider.GetType().Name;
            var stopwatch = Stopwatch.StartNew();
            var result = await RunSafelyAsync(provider, youTubeVideoId, cancellationToken);
            stopwatch.Stop();

            if (result.Outcome == TranscriptFetchOutcome.Fetched && result.Lines.Count > 0)
            {
                _logger.LogInformation(
                    "{Provider} returned {Count} transcript lines for video {VideoId} in {Ms}ms.",
                    name, result.Lines.Count, youTubeVideoId, stopwatch.ElapsedMilliseconds);
                return result;
            }

            if (result.Outcome == TranscriptFetchOutcome.ProviderUnavailable)
            {
                anyUnavailable = true;
                _logger.LogInformation(
                    "{Provider} could not return a transcript for video {VideoId} ({Ms}ms); trying the next source.",
                    name, youTubeVideoId, stopwatch.ElapsedMilliseconds);
            }
            else
            {
                _logger.LogInformation(
                    "{Provider} reports video {VideoId} has no captions; trying the next source.", name, youTubeVideoId);
            }
        }

        // No provider produced lines. Stay non-terminal if any provider failed to run (so a later open
        // retries); only settle on "no captions" when every provider ran and confirmed none exist.
        return anyUnavailable
            ? TranscriptFetchResult.ProviderUnavailable
            : TranscriptFetchResult.NoCaptions;
    }

    /// <summary>
    /// Runs one provider, guaranteeing the chain survives it: a thrown exception or a hang past the
    /// per-provider timeout is converted to <see cref="TranscriptFetchOutcome.ProviderUnavailable"/> so
    /// the next provider is tried. Caller-requested cancellation is propagated (not swallowed).
    /// </summary>
    private async Task<TranscriptFetchResult> RunSafelyAsync(
        IVideoTranscriptProvider provider, string youTubeVideoId, CancellationToken cancellationToken)
    {
        using var timeout = _perProviderTimeout is { } span
            ? CancellationTokenSource.CreateLinkedTokenSource(cancellationToken)
            : null;
        timeout?.CancelAfter(_perProviderTimeout!.Value);
        var token = timeout?.Token ?? cancellationToken;

        try
        {
            return await provider.FetchAsync(youTubeVideoId, token);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // The CALLER cancelled - abort the whole chain, do not mask it as a provider failure.
            throw;
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(
                "{Provider} timed out after {Timeout} for video {VideoId}; trying the next source.",
                provider.GetType().Name, _perProviderTimeout, youTubeVideoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }
        catch (Exception ex)
        {
            // A provider should never throw, but if one does the chain must not die: log and fall through.
            _logger.LogError(ex,
                "{Provider} threw while fetching video {VideoId}; treating as unavailable and continuing.",
                provider.GetType().Name, youTubeVideoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }
    }
}
