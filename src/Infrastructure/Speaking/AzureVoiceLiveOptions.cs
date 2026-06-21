namespace Infrastructure.Speaking;

public sealed class AzureVoiceLiveOptions
{
    public const string SectionName = "AzureVoiceLive";

    /// <summary>Voice Live realtime api-version. Pin to the version validated against the resource.</summary>
    public string ApiVersion { get; set; } = "2026-06-01-preview";

    /// <summary>Entra token scope for the Foundry resource.</summary>
    public string TokenScope { get; set; } = "https://ai.azure.com/.default";

    /// <summary>Realtime PCM sample rate the browser captures/plays (Voice Live default 24 kHz).</summary>
    public int InputSamplingRate { get; set; } = 24_000;

    /// <summary>Server VAD end-of-speech silence window.</summary>
    public int SilenceDurationMs { get; set; } = 500;

    /// <summary>Turn detection mode (server_vad = Azure detects end of speech automatically).</summary>
    public string TurnDetectionType { get; set; } = "server_vad";

    /// <summary>
    /// Rate used when <see cref="CostPerAudioHourUsd"/> is unset. Deliberately high: an
    /// unconfigured deployment must over-charge itself (and shed early) rather than bill a paid
    /// realtime service at zero. It is never meant to be accurate — set the real rate.
    /// </summary>
    public const double DefaultCostPerAudioHourUsd = 30;

    /// <summary>Mirrors AzureSpeech:PricingConfigured — asserts the operator set a real rate.</summary>
    public bool PricingConfigured { get; set; }

    /// <summary>Blended Voice Live realtime audio price (input + output) per audio hour.</summary>
    public double CostPerAudioHourUsd { get; set; }

    /// <summary>Hard ceiling on what one session can be billed, however long the tab stays open.</summary>
    public int MaxSessionMinutes { get; set; } = 10;

    /// <summary>
    /// Charged up front when the token is minted and reconciled when the session reports back.
    /// This is the whole exposure from a tab that crashes and never reports. Kept small because
    /// the daily budget is global, not per learner: every un-reconciled reservation eats into the
    /// budget that shields every other free user.
    /// </summary>
    public int ReservationMinutes { get; set; } = 2;

    /// <summary>Per-learner daily Voice Live allowance, in minutes.</summary>
    public int DailyMinutesPerLearner { get; set; } = 30;

    public double EffectiveCostPerAudioHourUsd =>
        CostPerAudioHourUsd > 0 ? CostPerAudioHourUsd : DefaultCostPerAudioHourUsd;

    /// <summary>
    /// Throws when the configuration would silently mis-bill. Unlike the LLM (which really is free
    /// today), Voice Live is never free, so declaring pricing configured with a zero rate is always
    /// a mistake rather than a legitimate state.
    /// </summary>
    public void Validate()
    {
        if (PricingConfigured && CostPerAudioHourUsd <= 0)
            throw new InvalidOperationException(
                $"{SectionName}:PricingConfigured is true but {SectionName}:CostPerAudioHourUsd is not set. "
                + "Voice Live is a paid realtime service; leaving the rate at zero would hide its cost "
                + "from the daily budget.");

        if (MaxSessionMinutes < 1)
            throw new InvalidOperationException($"{SectionName}:MaxSessionMinutes must be at least 1.");

        if (ReservationMinutes < 1 || ReservationMinutes > MaxSessionMinutes)
            throw new InvalidOperationException(
                $"{SectionName}:ReservationMinutes must be between 1 and MaxSessionMinutes ({MaxSessionMinutes}).");

        if (DailyMinutesPerLearner < MaxSessionMinutes)
            throw new InvalidOperationException(
                $"{SectionName}:DailyMinutesPerLearner must be at least MaxSessionMinutes ({MaxSessionMinutes}), "
                + "otherwise a learner could never complete a single full session.");
    }
}
