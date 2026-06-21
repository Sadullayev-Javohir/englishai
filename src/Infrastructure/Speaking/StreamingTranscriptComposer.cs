using Application.Speaking.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

/// <summary>
/// Turns the segments collected during a streaming recognition into a final
/// <see cref="SpeechTranscription"/>. Extracted from the session so the "fall back to the
/// last partial transcript" decision is unit-testable without the Azure SDK.
/// </summary>
/// <remarks>
/// Azure raises <c>Recognizing</c> (partial) events while audio streams and a final
/// <c>Recognized</c> event once it commits a segment. When the learner's turn is short and the
/// push stream is closed before Azure emits that final event, the session ends on
/// <c>EndOfStream</c> with no committed segment - even though a perfectly good partial
/// transcript existed. Rejecting that as "no speech" is exactly what surfaced to learners as
/// "I spoke but the tutor never replied". This composer uses the last partial as a fallback so
/// the turn still produces a reply.
/// </remarks>
internal static class StreamingTranscriptComposer
{
    public static SpeechTranscription Build(
        IReadOnlyList<IReadOnlyList<SpeechTranscriptionCandidate>> segments,
        string? lastPartialTranscript,
        SpeechRecognitionContext context,
        AzureSpeechOptions options,
        ILogger logger)
    {
        var effectiveSegments = segments;

        if (effectiveSegments.Count == 0)
        {
            if (string.IsNullOrWhiteSpace(lastPartialTranscript))
                return SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech);

            logger.LogInformation(
                "Streaming recognition produced no final segment; falling back to the last partial transcript.");

            // The partial is Azure's best interim hypothesis for the utterance. It clears the
            // acceptance floor so the turn produces a reply, and sits at the confirmation
            // threshold so a confident single hypothesis is not needlessly flagged for
            // confirmation.
            var fallbackConfidence = Math.Max(
                options.MinimumRecognitionConfidence,
                options.ConfirmationConfidenceThreshold);
            effectiveSegments = new IReadOnlyList<SpeechTranscriptionCandidate>[]
            {
                new[] { new SpeechTranscriptionCandidate(lastPartialTranscript.Trim(), fallbackConfidence) },
            };
        }

        var composed = TranscriptAlternativeComposer.Compose(
            effectiveSegments,
            Math.Max(2, options.MaximumTranscriptAlternatives));
        var selection = ContextualTranscriptSelector.Select(
            composed,
            context,
            options.ConfirmationConfidenceThreshold,
            options.ConfirmationScoreGap,
            options.MaximumTranscriptAlternatives);

        if (selection is null)
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.NoSpeech);
        if (selection.Confidence < options.MinimumRecognitionConfidence)
        {
            logger.LogInformation(
                "Streaming transcription rejected because confidence {Confidence:F3} is below {Minimum:F3}.",
                selection.Confidence,
                options.MinimumRecognitionConfidence);
            return SpeechTranscription.Rejected(SpeechTranscriptionRejection.LowConfidence);
        }

        return SpeechTranscription.Accepted(
            ContextualTranscriptCorrections.Apply(selection.Text, context),
            selection.Confidence,
            selection.Alternatives,
            selection.RequiresConfirmation,
            selection.ScoreGap);
    }
}
