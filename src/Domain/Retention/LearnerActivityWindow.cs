namespace Domain.Retention;

/// <summary>
/// A learner's registration day and the distinct calendar days on which they were active
/// (PROJECT-SPEC I.4). The unit for cohort retention math; assembled from the learner
/// profile's creation date and activity log by the Application layer.
/// </summary>
public sealed record LearnerActivityWindow(DateOnly RegisteredOn, IReadOnlyCollection<DateOnly> ActiveDays);
