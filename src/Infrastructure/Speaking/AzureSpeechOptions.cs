namespace Infrastructure.Speaking;

/// <summary>
/// Configuration for Azure Speech (STT / TTS / Pronunciation Assessment). Bound from
/// the "AzureSpeech" config section. Key + region come from user-secrets/env
/// (docs/development-guide.md rule 13) - never hardcoded. When unset, the deterministic Local
/// adapters are used instead.
/// </summary>
public sealed class AzureSpeechOptions
{
    public const string SectionName = "AzureSpeech";

    public string? Key { get; set; }
    public string? Region { get; set; }

    /// <summary>Recognition language for STT and pronunciation assessment.</summary>
    public string RecognitionLanguage { get; set; } = "en-US";

    /// <summary>
    /// Minimum detailed-result confidence accepted as the learner's speech. Kept deliberately
    /// low because Uzbek/L2 speakers of English routinely score 0.30-0.45 on genuine, intelligible
    /// utterances; a higher floor silently rejected real speech (observed 0.390 rejections in prod)
    /// and looked like "the tutor never replies". Speech between this floor and
    /// <see cref="ConfirmationConfidenceThreshold"/> is accepted for confirmation, not discarded.
    /// </summary>
    public double MinimumRecognitionConfidence { get; set; } = 0.30;

    /// <summary>Transcript confidence below which the learner confirms or corrects the text.</summary>
    public double ConfirmationConfidenceThreshold { get; set; } = 0.60;

    /// <summary>Minimum score gap between the best two contextual alternatives for auto-acceptance.</summary>
    public double ConfirmationScoreGap { get; set; } = 0.05;

    /// <summary>Maximum transcript alternatives returned to the learner.</summary>
    public int MaximumTranscriptAlternatives { get; set; } = 3;

    /// <summary>Phrase-grammar weight applied when the conversation genuinely expects a learner name.</summary>
    public double NamePhraseWeight { get; set; } = 1.35;

    /// <summary>Neural voice used for tutor TTS.</summary>
    public string VoiceName { get; set; } = "en-US-JennyNeural";

    public bool PricingConfigured { get; set; }
    public double SpeechToTextCostPerAudioHourUsd { get; set; }
    public double TextToSpeechCostPerMillionCharactersUsd { get; set; }
    public double PronunciationCostPerAudioHourUsd { get; set; }

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(Key) && !string.IsNullOrWhiteSpace(Region);
}
