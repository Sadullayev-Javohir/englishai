namespace Domain.Listening;

/// <summary>
/// Lifecycle of a topic-scoped listening exercise: <see cref="Pending"/> until its transcript and
/// comprehension questions have been generated and cached, then <see cref="Filled"/>. Legacy
/// stand-alone curated exercises are created already <see cref="Filled"/>.
/// </summary>
public enum ListeningExerciseStatus
{
    Pending = 0,
    Filled = 1,
}
