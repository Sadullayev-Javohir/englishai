namespace Domain.Subscription;

/// <summary>
/// Outcome of a topic-level paywall check (PROJECT-SPEC H.1 trial gate). A free learner may engage
/// the first <see cref="FreeTopicAccessPolicy.FreeTopicAllowance"/> topics of the whole curriculum;
/// every later topic requires Premium.
/// </summary>
/// <param name="IsAllowed">Whether the learner may open/practice the requested topic.</param>
/// <param name="RequiresPro">True when the topic is blocked purely because the free trial is used up
/// (so the client shows the upgrade paywall rather than a generic error).</param>
/// <param name="FreeAllowance">How many topics the free trial covers.</param>
/// <param name="StartedCount">How many distinct topics the learner has already started.</param>
public sealed record TopicAccessDecision(bool IsAllowed, bool RequiresPro, int FreeAllowance, int StartedCount);

/// <summary>
/// The trial paywall policy (PROJECT-SPEC H.1): pure logic deciding whether a learner may access a
/// given topic. A privileged learner (active Premium subscription, or a complimentary-allowlist
/// account) has unlimited access. A free learner is entitled to the first
/// <see cref="FreeTopicAllowance"/> topics they ever start; any further topic is locked until they
/// subscribe. This layers on top of the K.5 rolling topic window so the first three open topics are
/// also covered by the base free allowance.
/// </summary>
public static class FreeTopicAccessPolicy
{
    /// <summary>How many topics a free (non-Premium, non-comped) learner may engage before paying.</summary>
    public const int FreeTopicAllowance = 3;

    /// <summary>
    /// Decides access to <paramref name="requestedTopicId"/>.
    /// </summary>
    /// <param name="hasFullAccess">True for an active Premium subscription or a comped account.</param>
    /// <param name="startedTopicIdsEarliestFirst">
    /// The distinct topics the learner has already started, ordered oldest-first. The earliest
    /// <see cref="FreeTopicAllowance"/> of these form the learner's free set; a started topic outside
    /// that set (possible only after a lapsed Premium period) is treated as locked again.
    /// </param>
    /// <param name="bonusTopicAllowance">
    /// Extra topics unlocked beyond the base free trial - e.g. the +2 topics earned per qualified
    /// referral. Added on top of <see cref="FreeTopicAllowance"/>; defaults
    /// to 0 so every existing (non-referral) caller keeps the plain three-topic trial.
    /// </param>
    public static TopicAccessDecision Evaluate(
        bool hasFullAccess,
        IReadOnlyList<Guid> startedTopicIdsEarliestFirst,
        Guid requestedTopicId,
        int bonusTopicAllowance = 0)
    {
        var startedCount = startedTopicIdsEarliestFirst.Count;
        var allowance = FreeTopicAllowance + Math.Max(0, bonusTopicAllowance);

        if (hasFullAccess)
            return new TopicAccessDecision(IsAllowed: true, RequiresPro: false, allowance, startedCount);

        // The free set is the learner's earliest-started topics, capped at the (possibly boosted) allowance.
        var freeSet = new HashSet<Guid>();
        for (var i = 0; i < startedCount && i < allowance; i++)
            freeSet.Add(startedTopicIdsEarliestFirst[i]);

        // Allowed if it is one of the free topics, or there is still room to start a new free topic.
        var allowed = freeSet.Contains(requestedTopicId) || startedCount < allowance;
        return new TopicAccessDecision(allowed, RequiresPro: !allowed, allowance, startedCount);
    }
}
