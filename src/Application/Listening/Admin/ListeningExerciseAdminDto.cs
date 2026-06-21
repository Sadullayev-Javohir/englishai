using Domain.Listening;

namespace Application.Listening.Admin;

/// <summary>
/// A listening exercise row for the admin catalog (PROJECT-SPEC module - Listening). The curated
/// metadata plus computed display fields (level/status as strings, question/word counts).
/// </summary>
public sealed record ListeningExerciseAdminDto(
    Guid Id,
    string Title,
    string Topic,
    string Level,
    string Status,
    int QuestionCount,
    int WordCount,
    Guid? VocabularyTopicId,
    DateTimeOffset CreatedAt)
{
    /// <summary>Builds the admin row from the aggregate (status/level as human-readable strings).</summary>
    public static ListeningExerciseAdminDto FromDomain(ListeningExercise exercise) =>
        new(
            exercise.Id,
            exercise.Title,
            exercise.Topic,
            exercise.Level.ToString(),
            exercise.Status.ToString(),
            exercise.Questions.Count,
            exercise.WordCount,
            exercise.VocabularyTopicId,
            exercise.CreatedAt);
}
