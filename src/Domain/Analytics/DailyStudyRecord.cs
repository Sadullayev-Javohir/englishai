using Domain.Common;
using Domain.Learning;

namespace Domain.Analytics;

/// <summary>
/// How long a learner studied on a single calendar day, broken down by the six core skills
/// (PROJECT-SPEC Faza 2 analitika). One durable row per (learner, day): the heartbeat from the
/// active learning page accumulates seconds here, and the progress dashboard aggregates these
/// rows into today/week/month/year/all-time totals via <see cref="StudyStatsCalculator"/>.
///
/// The day is the learner's <em>local</em> calendar day (supplied by the client), so "today"
/// and the week/month/year boundaries line up with what the learner sees on their own clock
/// rather than UTC.
/// </summary>
public sealed class DailyStudyRecord
{
    // A single heartbeat can add at most this many seconds - bounds runaway/abusive clients
    // (a tab left open should still only credit real, capped increments - docs/development-guide.md rule 10).
    public const int MaxHeartbeatSeconds = 120;

    // Parameterless ctor for EF Core materialization.
    private DailyStudyRecord()
    {
    }

    private DailyStudyRecord(Guid learnerId, DateOnly day)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Day = day;
    }

    public static DailyStudyRecord Start(Guid learnerId, DateOnly day)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("A study record needs a learner.");

        return new DailyStudyRecord(learnerId, day);
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public DateOnly Day { get; private set; }

    public int SpeakingSeconds { get; private set; }
    public int ListeningSeconds { get; private set; }
    public int ReadingSeconds { get; private set; }
    public int WritingSeconds { get; private set; }
    public int GrammarSeconds { get; private set; }
    public int VocabularySeconds { get; private set; }

    /// <summary>Total seconds studied this day across all skills (computed, not stored).</summary>
    public int TotalSeconds =>
        SpeakingSeconds + ListeningSeconds + ReadingSeconds +
        WritingSeconds + GrammarSeconds + VocabularySeconds;

    /// <summary>
    /// Credits one heartbeat of study time to a skill. <paramref name="seconds"/> must be a
    /// positive, capped increment (<see cref="MaxHeartbeatSeconds"/>); larger values are clamped
    /// so a single call can never inflate the total.
    /// </summary>
    public void AddTime(SkillType skill, int seconds)
    {
        if (seconds <= 0)
            throw new DomainException("Study time must be a positive number of seconds.");

        var credited = Math.Min(seconds, MaxHeartbeatSeconds);

        switch (skill)
        {
            case SkillType.Speaking: SpeakingSeconds += credited; break;
            case SkillType.Listening: ListeningSeconds += credited; break;
            case SkillType.Reading: ReadingSeconds += credited; break;
            case SkillType.Writing: WritingSeconds += credited; break;
            case SkillType.Grammar: GrammarSeconds += credited; break;
            case SkillType.Vocabulary: VocabularySeconds += credited; break;
            default: throw new DomainException($"Unknown skill: {skill}.");
        }
    }

    public int SecondsFor(SkillType skill) => skill switch
    {
        SkillType.Speaking => SpeakingSeconds,
        SkillType.Listening => ListeningSeconds,
        SkillType.Reading => ReadingSeconds,
        SkillType.Writing => WritingSeconds,
        SkillType.Grammar => GrammarSeconds,
        SkillType.Vocabulary => VocabularySeconds,
        _ => 0,
    };
}
