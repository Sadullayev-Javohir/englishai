using Domain.Common;

namespace Domain.Vocabulary;

/// <summary>
/// The spaced-repetition schedule for a single learned word (PROJECT-SPEC B.1). It
/// implements the exact 3/7/21-day ladder: each interval is measured from the day the
/// word was learned, a passed review advances to the next checkpoint, and a failed
/// review restarts the whole schedule from day 0 (incrementing the fail count). The
/// three intervals are named constants below.
/// </summary>
public sealed class ReviewSchedule
{
    /// <summary>Days from learning to the first review.</summary>
    public const int Day3IntervalDays = 3;

    /// <summary>Days from learning to the second review.</summary>
    public const int Day7IntervalDays = 7;

    /// <summary>Days from learning to the final review.</summary>
    public const int Day21IntervalDays = 21;

    // Parameterless ctor for EF Core materialization.
    private ReviewSchedule()
    {
    }

    private ReviewSchedule(DateTimeOffset learnedAt)
    {
        Id = Guid.NewGuid();
        Restart(learnedAt);
    }

    public Guid Id { get; private set; }

    /// <summary>When the current schedule cycle started (day 0). Resets on a failed review.</summary>
    public DateTimeOffset LearnedAt { get; private set; }

    /// <summary>When the next checkpoint or maintenance review is due, or null when unscheduled.</summary>
    public DateTimeOffset? NextReviewAt { get; private set; }

    public ReviewStage Stage { get; private set; }

    /// <summary>How many times the learner has failed a review and restarted the cycle.</summary>
    public int FailCount { get; private set; }
    public int ReviewCount { get; private set; }
    public int? LastResponseLatencyMs { get; private set; }
    public int LastHintCount { get; private set; }

    /// <summary>Starts a fresh schedule for a word learned at <paramref name="learnedAt"/>.</summary>
    public static ReviewSchedule StartNew(DateTimeOffset learnedAt) => new(learnedAt);

    /// <summary>
    /// Forces the word to be due for review right now, without disturbing its learning history
    /// (stage / fail count are preserved). A testing/operator aid: a freshly saved word's first
    /// review is days out, which leaves the review screen empty, so this pulls the learner's words
    /// into the SRS queue immediately so the flow can be exercised. A mastered word is revived to
    /// the first checkpoint so it, too, becomes reviewable again.
    /// </summary>
    public void MakeDueNow(DateTimeOffset now)
    {
        if (Stage == ReviewStage.Mastered)
            Stage = ReviewStage.Day3;

        NextReviewAt = now;
    }

    /// <summary>True when the next scheduled checkpoint or maintenance review has become due.</summary>
    public bool IsDue(DateTimeOffset now) => NextReviewAt is { } due && due <= now;

    /// <summary>
    /// Applies the outcome of a review. A pass advances to the next checkpoint (or marks
    /// the word mastered after the 21-day review); a failure restarts the cycle from now
    /// and increments <see cref="FailCount"/>.
    /// </summary>
    public void RecordResult(bool passed, DateTimeOffset now, int? responseLatencyMs = null, int hintCount = 0)
    {
        if (responseLatencyMs is < 0)
            throw new DomainException("Response latency must not be negative.");
        if (hintCount < 0)
            throw new DomainException("Hint count must not be negative.");
        ReviewCount++;
        LastResponseLatencyMs = responseLatencyMs;
        LastHintCount = hintCount;

        if (Stage == ReviewStage.Mastered)
        {
            if (passed)
                NextReviewAt = now.AddDays(180);
            else
            {
                FailCount++;
                Stage = ReviewStage.Day21;
                NextReviewAt = now.AddDays(Day7IntervalDays);
            }
            return;
        }

        if (!passed)
        {
            FailCount++;
            Restart(now);
            return;
        }

        switch (Stage)
        {
            case ReviewStage.Day3:
                Stage = ReviewStage.Day7;
                NextReviewAt = LearnedAt.AddDays(Day7IntervalDays);
                break;
            case ReviewStage.Day7:
                Stage = ReviewStage.Day21;
                NextReviewAt = LearnedAt.AddDays(Day21IntervalDays);
                break;
            case ReviewStage.Day21:
                Stage = ReviewStage.Mastered;
                NextReviewAt = now.AddDays(90);
                break;
        }
    }

    private void Restart(DateTimeOffset learnedAt)
    {
        LearnedAt = learnedAt;
        Stage = ReviewStage.Day3;
        NextReviewAt = learnedAt.AddDays(Day3IntervalDays);
    }
}
