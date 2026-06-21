using Application.Ai;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Infrastructure.Ai;

namespace Infrastructure.Speaking;

/// <summary>
/// Serves repeated phrases from cache instead of paying Azure per character again (docs/development-guide.md
/// rule 10). Conversation openings, roleplay openers and stock tutor lines are identical for every
/// learner, so they are synthesized once for the whole platform.
///
/// The cache sits OUTSIDE <see cref="AzureTextToSpeechService.SynthesizeWithVoiceAsync"/>, which is
/// where admission control and cost metering live. That is deliberate on both counts: a hit incurs
/// no Azure spend, so it must not be billed, and it must not be shed when the daily budget is
/// exhausted - a free learner whose budget ran out can still hear an already-paid-for line. A hit is
/// still recorded, at zero cost, under <see cref="VariableCostCategory.TextToSpeechCached"/> so the
/// traffic stays visible on the founder dashboard rather than silently disappearing from the report.
/// </summary>
public sealed class CachedTextToSpeechService(
    IVoicedTextToSpeechService inner,
    ISynthesizedSpeechCache cache,
    AzureSpeechOptions options,
    IVariableCostMeter costs) : IVoicedTextToSpeechService
{
    public Task<SynthesizedSpeech> SynthesizeAsync(string text, CancellationToken cancellationToken = default) =>
        SynthesizeWithVoiceAsync(text, options.VoiceName, cancellationToken);

    public async Task<SynthesizedSpeech> SynthesizeWithVoiceAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);

        var cached = await cache.GetAsync(voiceName, text, cancellationToken);
        if (cached is not null)
        {
            RecordHit(text);
            return cached;
        }

        var speech = await inner.SynthesizeWithVoiceAsync(text, voiceName, cancellationToken);
        await cache.SetAsync(voiceName, text, speech, cancellationToken);
        return speech;
    }

    private void RecordHit(string text)
    {
        var admission = AiAdmissionContext.Current;
        costs.Record(
            VariableCostCategory.TextToSpeechCached,
            text.Length,
            "character",
            estimatedCostUsd: 0,
            admission.CallerKey,
            AiFeature.SpeakingTutor,
            admission.RequestPath);
    }
}
