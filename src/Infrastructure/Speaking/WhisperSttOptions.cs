namespace Infrastructure.Speaking;

/// <summary>
/// The self-hosted speech-to-text sidecar used for free accounts (services/speech-stt).
///
/// Azure Speech bills per audio hour, which no realistic free-tier conversion rate can carry. Free
/// learners are transcribed on our own VPS instead, where a request costs CPU time we have already
/// paid for; Premium keeps Azure, whose accented-speech accuracy and pronunciation scoring are what
/// the subscription actually buys (docs/development-guide.md rule 10).
/// </summary>
public sealed class WhisperSttOptions
{
    public const string SectionName = "WhisperStt";

    /// <summary>
    /// Off by default. An unconfigured deployment keeps sending every learner to Azure, which costs
    /// money but works - the safe direction for a fallback that has not been proven on the box yet.
    /// </summary>
    public bool Enabled { get; set; }

    public string BaseUrl { get; set; } = "http://speech-stt:8080";

    /// <summary>
    /// Generous, because CPU transcription is slower than a cloud call: base.en runs at roughly
    /// 0.3-0.6x realtime, so a long turn legitimately takes several seconds.
    /// </summary>
    public int TimeoutSeconds { get; set; } = 30;

    /// <summary>
    /// Minimum accepted confidence. Deliberately its OWN setting rather than reusing
    /// <see cref="AzureSpeechOptions.MinimumRecognitionConfidence"/>: this value comes from the
    /// decoder's average log-probability and is not on the same scale, so sharing a threshold would
    /// silently reject or accept the wrong utterances.
    /// </summary>
    public double MinimumConfidence { get; set; } = 0.25;

    public bool IsConfigured =>
        Enabled && Uri.TryCreate(BaseUrl, UriKind.Absolute, out _);
}
