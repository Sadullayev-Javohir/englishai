namespace Application.Gamification.Dtos;

/// <summary>What a consume attempt did, so the client never has to guess why nothing was spent.</summary>
public enum EnergyOutcome
{
    /// <summary>No attempt was made - this is a plain balance read.</summary>
    None = 0,

    /// <summary>One unit was debited.</summary>
    Consumed = 1,

    /// <summary>The learner had already paid for this exact activity; re-opening is free.</summary>
    AlreadyStarted = 2,

    /// <summary>The bar was empty, so nothing was debited and the activity must not start.</summary>
    Insufficient = 3,
}

/// <param name="Current">Units available right now.</param>
/// <param name="Maximum">Cap, always <see cref="Domain.Gamification.EnergyPolicy.MaximumEnergy"/>.</param>
/// <param name="NextRefillAt">When the next single unit lands; null at a full bar.</param>
/// <param name="FullRefillAt">When the bar is full again; null at a full bar.</param>
/// <param name="Outcome">Result of the consume attempt, or <see cref="EnergyOutcome.None"/> for a read.</param>
public sealed record EnergyDto(
    int Current,
    int Maximum,
    DateTimeOffset? NextRefillAt,
    DateTimeOffset? FullRefillAt,
    EnergyOutcome Outcome);
