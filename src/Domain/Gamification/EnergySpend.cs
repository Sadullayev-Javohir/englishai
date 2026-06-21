using Domain.Common;

namespace Domain.Gamification;

/// <summary>
/// One "this learner already paid for this" receipt. Its existence is what makes re-opening a
/// video or reconnecting to a conversation free: the spend is keyed by
/// (<see cref="LearnerId"/>, <see cref="Action"/>, <see cref="ReferenceId"/>) with a unique
/// index, so a second attempt finds the receipt instead of debiting the bar again.
/// </summary>
public sealed class EnergySpend
{
    private EnergySpend()
    {
        ReferenceId = string.Empty;
    }

    private EnergySpend(Guid learnerId, EnergyAction action, string referenceId, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Action = action;
        ReferenceId = referenceId;
        SpentAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public EnergyAction Action { get; private set; }

    /// <summary>What was started: the YouTube video id, or the conversation session id.</summary>
    public string ReferenceId { get; private set; }

    public DateTimeOffset SpentAt { get; private set; }

    public static EnergySpend Record(
        Guid learnerId, EnergyAction action, string referenceId, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (string.IsNullOrWhiteSpace(referenceId))
            throw new DomainException("Energy spend reference must not be empty.");

        return new EnergySpend(learnerId, action, referenceId.Trim(), now);
    }
}
