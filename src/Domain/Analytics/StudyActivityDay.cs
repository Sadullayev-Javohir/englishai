namespace Domain.Analytics;

/// <summary>
/// A single (learner, day) fact that the learner recorded study time - the engagement signal the
/// founder analytics dashboard aggregates into DAU/WAU/MAU and cohort retention. Deliberately a
/// lightweight projection of <see cref="DailyStudyRecord"/> (no per-skill seconds) so the whole
/// user base can be read in one query without materializing full rows.
///
/// The <see cref="Day"/> is the learner's local calendar day (as stored on the record), so the
/// active-user windows line up with what learners experience on their own clock.
/// </summary>
public readonly record struct StudyActivityDay(Guid LearnerId, DateOnly Day);
