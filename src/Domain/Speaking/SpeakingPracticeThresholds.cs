namespace Domain.Speaking;

public static class SpeakingPracticeThresholds
{
    public const double MasteryScore = 85.0;

    // A word leaves the practice queue only after the learner clears the mastery score
    // this many times, so a single lucky attempt no longer removes it.
    public const int RequiredSuccesses = 3;
}
