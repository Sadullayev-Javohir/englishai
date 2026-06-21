using Domain.Common;

namespace Domain.Competition;

/// <summary>
/// Value object carrying the host-tunable settings of a competition. Immutable once set at
/// creation; the host may not change rules mid-game (keeps the real-time flow deterministic).
/// </summary>
public sealed record CompetitionSettings
{
    /// <summary>Seconds each slide is shown before auto-advance (host-configurable). Default 30.</summary>
    public int SlideDurationSeconds { get; }

    /// <summary>When true, the slide advances automatically when its timer expires.</summary>
    public bool AutoAdvance { get; }

    /// <summary>When true, the generated slide order is shuffled (otherwise topic order, then vocab→grammar).</summary>
    public bool ShuffleSlides { get; }

    /// <summary>When true, learners may join after the host has started the competition.</summary>
    public bool AllowLateJoin { get; }

    /// <summary>How many questions are pulled from each selected topic (vocab + grammar). Default 3.</summary>
    public int QuestionsPerTopic { get; }

    /// <summary>When true, the live leaderboard is broadcast during the game.</summary>
    public bool ShowLiveLeaderboard { get; }

    /// <summary>Points awarded for a correct answer (scaled by speed bonus on submit). Default 10.</summary>
    public int BasePointsPerCorrect { get; }

    public const int DefaultSlideDurationSeconds = 30;
    public const int DefaultQuestionsPerTopic = 3;
    public const int DefaultBasePointsPerCorrect = 10;
    public const int MaxSlideDurationSeconds = 120;
    public const int MaxQuestionsPerTopic = 10;

    public CompetitionSettings(
        int slideDurationSeconds = DefaultSlideDurationSeconds,
        bool autoAdvance = true,
        bool shuffleSlides = true,
        bool allowLateJoin = false,
        int questionsPerTopic = DefaultQuestionsPerTopic,
        bool showLiveLeaderboard = true,
        int basePointsPerCorrect = DefaultBasePointsPerCorrect)
    {
        if (slideDurationSeconds < 5 || slideDurationSeconds > MaxSlideDurationSeconds)
            throw new DomainException(
                $"Slide duration must be between 5 and {MaxSlideDurationSeconds} seconds.");
        if (questionsPerTopic < 1 || questionsPerTopic > MaxQuestionsPerTopic)
            throw new DomainException(
                $"Questions per topic must be between 1 and {MaxQuestionsPerTopic}.");
        if (basePointsPerCorrect < 1)
            throw new DomainException("Base points per correct answer must be at least 1.");

        SlideDurationSeconds = slideDurationSeconds;
        AutoAdvance = autoAdvance;
        ShuffleSlides = shuffleSlides;
        AllowLateJoin = allowLateJoin;
        QuestionsPerTopic = questionsPerTopic;
        ShowLiveLeaderboard = showLiveLeaderboard;
        BasePointsPerCorrect = basePointsPerCorrect;
    }

    /// <summary>Speed bonus: faster correct answers earn up to +50% of base. ratio∈[0,1] from timer.</summary>
    public int PointsForCorrect(double timeRatioRemaining)
    {
        var ratio = Math.Clamp(timeRatioRemaining, 0d, 1d);
        return BasePointsPerCorrect + (int)Math.Round(BasePointsPerCorrect * 0.5 * ratio);
    }
}
