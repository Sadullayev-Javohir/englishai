namespace Application.Ai;

public enum AiFeature
{
    Assistant,
    SpeakingTutor,
    SpeakingEvaluation,
    Translation,
    WritingGeneration,
    WritingAssessment,
    VideoExplain,
    Other,
}

public enum AiSubscriptionTier
{
    Free,
    Pro,
}

public sealed record AiAdmissionRequest(
    string CallerKey,
    AiSubscriptionTier Tier,
    AiFeature Feature,
    int EstimatedInputTokens,
    int MaxOutputTokens);

public sealed record AiAdmissionSnapshot(
    int Active,
    int Queued,
    long Admitted,
    long Completed,
    long Rejected,
    long TimedOut,
    long Cancelled,
    long ProviderUnavailable,
    long Fallbacks,
    long Requests,
    long EstimatedInputTokens,
    long EstimatedOutputTokens,
    double EstimatedCostUsd,
    double DailyBudgetUsedUsd,
    double DailyBudgetLimitUsd,
    bool DailyBudgetWarning,
    bool FreeTierShed,
    bool PricingConfigured);

public static class VariableCostCategory
{
    public const string Ai = "ai";
    public const string SpeechToText = "speech_to_text";

    /// <summary>
    /// Speech-to-text served by the self-hosted sidecar. Recorded at zero cost - it consumes CPU we
    /// already pay for - but recorded, so the volume the free tier moved off the metered provider is
    /// visible instead of disappearing from the report.
    /// </summary>
    public const string SpeechToTextSelfHosted = "speech_to_text_self_hosted";
    public const string TextToSpeech = "text_to_speech";

    /// <summary>
    /// Speech served from the synthesis cache. Recorded separately, at zero cost, so the founder
    /// dashboard shows the cache hit rate next to the spend it avoided instead of the traffic simply
    /// vanishing from the report.
    /// </summary>
    public const string TextToSpeechCached = "text_to_speech_cached";

    public const string Pronunciation = "pronunciation";

    /// <summary>
    /// Realtime Azure Voice Live sessions (accent tutors). The browser talks straight to Azure, so
    /// the cost is reserved when the token is minted and reconciled when the session reports back.
    /// </summary>
    public const string VoiceLive = "voice_live";
}

public sealed record VariableCostCategorySnapshot(
    string Category,
    long Requests,
    double Units,
    string Unit,
    double EstimatedCostUsd);

public sealed record VariableCostAttributionSnapshot(
    string Key,
    long Requests,
    double EstimatedCostUsd);

public sealed record VariableCostSnapshot(
    DateOnly Day,
    double DailyBudgetUsedUsd,
    double DailyBudgetLimitUsd,
    bool DailyBudgetWarning,
    bool FreeTierShed,
    bool PricingConfigured,
    IReadOnlyList<VariableCostCategorySnapshot> Categories,
    IReadOnlyList<VariableCostAttributionSnapshot> TopCallers,
    IReadOnlyList<VariableCostAttributionSnapshot> TopEndpoints);

public interface IVariableCostMeter
{
    VariableCostSnapshot Snapshot();
    Task SetDailyBudgetAsync(double dailyBudgetUsd, CancellationToken cancellationToken = default);
    void EnsureAllowed(AiSubscriptionTier tier, AiFeature feature, string? callerKey = null);
    void Record(
        string category,
        double units,
        string unit,
        double estimatedCostUsd,
        string? callerKey = null,
        AiFeature feature = AiFeature.Other,
        string? requestPath = null);

    /// <summary>
    /// Corrects an amount that <see cref="Record"/> already booked. Unlike <see cref="Record"/> the
    /// deltas may be negative (that is the point: a reservation is usually larger than the real
    /// usage) and no new request is counted, because the request was counted when it was reserved.
    /// </summary>
    void RecordAdjustment(
        string category,
        double unitsDelta,
        string unit,
        double estimatedCostDeltaUsd,
        string? callerKey = null,
        AiFeature feature = AiFeature.Other,
        string? requestPath = null);
}

public interface IAiAdmissionControl
{
    Task<IAiAdmissionLease> AcquireAsync(AiAdmissionRequest request, CancellationToken cancellationToken);
    void RecordProviderUnavailable(AiFeature feature, AiSubscriptionTier tier);
    void RecordFallback(AiFeature feature, AiSubscriptionTier tier);
    AiAdmissionSnapshot Snapshot();
}

public interface IAiAdmissionLease : IAsyncDisposable
{
    AiAdmissionRequest Request { get; }
    void Complete(int estimatedOutputTokens);
}

public sealed class AiAdmissionException(
    string code,
    string message,
    int retryAfterSeconds,
    int statusCode) : Exception(message)
{
    public string Code { get; } = code;
    public int RetryAfterSeconds { get; } = retryAfterSeconds;
    public int StatusCode { get; } = statusCode;
}
