using Domain.Subscription;

namespace Application.Subscription.Entitlements;

/// <summary>
/// The learner's daily speaking budget. Separate from <see cref="IEntitlementService"/> because it
/// meters a continuous quantity (minutes of speech) rather than counting discrete uses, and because
/// speech-to-text is billed per second - a session count would let one learner cost thirty times
/// another while both show as "one session".
/// </summary>
public interface ISpeakingMinuteAllowanceService
{
    /// <summary>How much of today's budget is left, without consuming any of it.</summary>
    Task<SpeakingMinuteDecision> EvaluateAsync(Guid learnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Throws <see cref="Common.SpeakingMinutesExhaustedException"/> when the budget is spent.
    /// Call before sending audio to speech-to-text, never after: the point is not to pay for it.
    /// </summary>
    Task EnsureAllowedAsync(Guid learnerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Books speech that was actually transcribed, and returns the updated decision so the caller can
    /// show the learner what is left. Recorded after acceptance, so a rejected or silent clip - which
    /// the learner cannot help - does not eat their budget.
    /// </summary>
    Task<SpeakingMinuteDecision> RecordAsync(
        Guid learnerId,
        double minutes,
        CancellationToken cancellationToken = default);
}
