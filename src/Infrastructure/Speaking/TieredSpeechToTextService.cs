using Application.Ai;
using Application.Speaking.Ports;
using Infrastructure.Ai;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

/// <summary>
/// Routes transcription by subscription tier: free accounts to the self-hosted sidecar, paying
/// accounts to Azure.
///
/// This is a product line as much as a cost control. Azure's accented-speech accuracy, its N-best
/// alternatives (the transcript-confirmation flow) and its pronunciation scoring are what a
/// subscription buys; the free tier gets a working conversation partner at a marginal cost the
/// platform can actually carry (docs/development-guide.md rule 10).
///
/// A sidecar failure falls back to Azure. Paying for a turn is far better than telling a learner
/// their speech could not be heard.
/// </summary>
public sealed class TieredSpeechToTextService : IContextualSpeechToTextService
{
    private readonly IContextualSpeechToTextService _selfHosted;
    private readonly IContextualSpeechToTextService _metered;
    private readonly ILogger<TieredSpeechToTextService> _logger;
    private int _consecutiveSelfHostedFailures;

    public TieredSpeechToTextService(
        IContextualSpeechToTextService selfHosted,
        IContextualSpeechToTextService metered,
        ILogger<TieredSpeechToTextService> logger)
    {
        _selfHosted = selfHosted;
        _metered = metered;
        _logger = logger;
    }

    public Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent, CancellationToken cancellationToken = default) =>
        TranscribeAsync(audioContent, EmptyContext, cancellationToken);

    public async Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent,
        SpeechRecognitionContext context,
        CancellationToken cancellationToken = default)
    {
        if (AiAdmissionContext.Current.Tier == AiSubscriptionTier.Pro)
            return await _metered.TranscribeAsync(audioContent, context, cancellationToken);

        var transcription = await _selfHosted.TranscribeAsync(audioContent, context, cancellationToken);
        if (transcription.Rejection != SpeechTranscriptionRejection.ServiceFailure)
        {
            Interlocked.Exchange(ref _consecutiveSelfHostedFailures, 0);
            return transcription;
        }

        // Logged at Warning and counted, because a dead sidecar silently shunting every free learner
        // to the metered provider is exactly the kind of failure that shows up as a surprise invoice
        // rather than as an outage.
        var failures = Interlocked.Increment(ref _consecutiveSelfHostedFailures);
        _logger.LogWarning(
            "Self-hosted speech-to-text unavailable ({Failures} consecutive); falling back to the metered provider.",
            failures);

        return await _metered.TranscribeAsync(audioContent, context, cancellationToken);
    }

    private static readonly SpeechRecognitionContext EmptyContext =
        new(null, null, Array.Empty<string>(), Array.Empty<string>(), false);
}
