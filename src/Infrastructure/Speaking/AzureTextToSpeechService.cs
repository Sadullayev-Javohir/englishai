using System.Security;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Ai;
using Infrastructure.Ai;
using Microsoft.CognitiveServices.Speech;
using DomainSpeaking = Domain.Speaking;

namespace Infrastructure.Speaking;

/// <summary>
/// Azure Neural TTS adapter that also collects the viseme track emitted during
/// synthesis (PROJECT-SPEC B.2), returning audio plus a <see cref="DomainSpeaking.VisemeSequence"/>
/// synchronized to it. Requests the official 2D red-lips SVG output via the
/// <c>mstts:viseme</c> SSML element so each viseme frame carries a self-contained,
/// ready-to-render mouth animation (Microsoft Learn: "Get facial position with viseme").
/// </summary>
public sealed class AzureTextToSpeechService : IVoicedTextToSpeechService
{
    private readonly AzureSpeechOptions _options;
    private readonly IVariableCostMeter _costs;

    public AzureTextToSpeechService(AzureSpeechOptions options, IVariableCostMeter costs)
    {
        _options = options;
        _costs = costs;
    }

    public async Task<SynthesizedSpeech> SynthesizeAsync(string text, CancellationToken cancellationToken = default)
        => await SynthesizeWithVoiceAsync(text, _options.VoiceName, cancellationToken);

    public async Task<SynthesizedSpeech> SynthesizeWithVoiceAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        var admission = AiAdmissionContext.Current;
        _costs.EnsureAllowed(admission.Tier, AiFeature.SpeakingTutor, admission.CallerKey);
        var characters = text.Length;

        try
        {
            return await SynthesizeCoreAsync(text, voiceName, cancellationToken);
        }
        finally
        {
            _costs.Record(
                VariableCostCategory.TextToSpeech,
                characters,
                "character",
                characters * Math.Max(0, _options.TextToSpeechCostPerMillionCharactersUsd) / 1_000_000d,
                admission.CallerKey,
                AiFeature.SpeakingTutor,
                admission.RequestPath);
        }
    }

    private async Task<SynthesizedSpeech> SynthesizeCoreAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken)
    {
        var speechConfig = SpeechConfig.FromSubscription(_options.Key, _options.Region);
        speechConfig.SpeechSynthesisVoiceName = voiceName;

        var frames = new List<DomainSpeaking.VisemeFrame>();
        var wordTimings = new List<SpeechWordTiming>();
        var plainTextOffset = 0;
        // redlips_front delivers ONE complete SVG (with internal SMIL timing for every viseme)
        // on a single VisemeReceived event near the end of the utterance - not one SVG per
        // frame. Capture it as the whole-sequence animation; it must survive independently of
        // the per-frame offset filtering below (that event's offset can fall past AudioDuration).
        string? animationSvg = null;

        using var synthesizer = new SpeechSynthesizer(speechConfig, audioConfig: null);
        synthesizer.VisemeReceived += (_, e) =>
        {
            if (!string.IsNullOrEmpty(e.Animation))
                animationSvg = e.Animation;
            // AudioOffset is in 100-nanosecond ticks, which TimeSpan.FromTicks expects.
            frames.Add(new DomainSpeaking.VisemeFrame(
                (int)e.VisemeId,
                TimeSpan.FromTicks((long)e.AudioOffset)));
        };
        synthesizer.WordBoundary += (_, e) =>
        {
            if (e.BoundaryType != SpeechSynthesisBoundaryType.Word || string.IsNullOrWhiteSpace(e.Text))
                return;

            var textOffset = text.IndexOf(e.Text, plainTextOffset, StringComparison.OrdinalIgnoreCase);
            if (textOffset < 0)
                return;
            plainTextOffset = textOffset + e.Text.Length;

            wordTimings.Add(new SpeechWordTiming(
                e.Text,
                textOffset,
                e.Text.Length,
                TimeSpan.FromTicks((long)e.AudioOffset),
                e.Duration));
        };

        using var result = await synthesizer.SpeakSsmlAsync(BuildSsml(text, voiceName)).ConfigureAwait(false);

        var duration = result.AudioDuration;
        if (duration <= TimeSpan.Zero)
        {
            var lastOffset = frames.Count > 0 ? frames[^1].AudioOffset : TimeSpan.Zero;
            duration = lastOffset + TimeSpan.FromMilliseconds(100);
        }

        // Drop any frame that rounds past the reported duration so the sequence
        // invariant (frames within audio length) always holds.
        var safeDuration = duration;
        var safeFrames = frames.Where(f => f.AudioOffset <= safeDuration).ToList();
        if (safeFrames.Count == 0)
            safeFrames.Add(new DomainSpeaking.VisemeFrame(0, TimeSpan.Zero));

        var visemes = DomainSpeaking.VisemeSequence.Create(safeFrames, duration, animationSvg);
        return new SynthesizedSpeech(
            result.AudioData,
            visemes,
            WordTimings: wordTimings.OrderBy(timing => timing.AudioOffset).ToList());
    }

    /// <summary>
    /// Wraps the text in SSML that requests the 2D red-lips SVG viseme output
    /// (<c>&lt;mstts:viseme type="redlips_front"/&gt;</c>). SVG output is produced only for
    /// the <c>en-US</c> locale, so the speak element's <c>xml:lang</c> follows the voice's
    /// locale (English-learning voices are en-US). The text is XML-escaped.
    /// </summary>
    private static string BuildSsml(string text, string voiceName)
    {
        var locale = LocaleFromVoice(voiceName);
        return
            $"<speak version=\"1.0\" xmlns=\"http://www.w3.org/2001/10/synthesis\" " +
            $"xmlns:mstts=\"http://www.w3.org/2001/mstts\" xml:lang=\"{locale}\">" +
            $"<voice name=\"{SecurityElement.Escape(voiceName)}\">" +
            "<mstts:viseme type=\"redlips_front\"/>" +
            SecurityElement.Escape(text) +
            "</voice></speak>";
    }

    // "en-US-JennyNeural" -> "en-US". Falls back to en-US (the only SVG-capable locale).
    private static string LocaleFromVoice(string? voiceName)
    {
        if (string.IsNullOrWhiteSpace(voiceName))
            return "en-US";
        var parts = voiceName.Split('-');
        return parts.Length >= 2 ? $"{parts[0]}-{parts[1]}" : "en-US";
    }
}
