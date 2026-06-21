using Domain.Grammar;

namespace Application.Grammar.Admin;

/// <summary>
/// A grammar-lesson row for the admin catalog (PROJECT-SPEC module 4) - the curated metadata
/// plus computed display fields (level/category/status as strings, exercise count). Distinct
/// from the learner catalog <c>GrammarLessonSummaryDto</c>, which layers mastery/gating on top.
/// </summary>
public sealed record GrammarLessonAdminDto(
    Guid Id,
    string Title,
    string Category,
    string Level,
    string Status,
    int ExerciseCount,
    Guid? VocabularyTopicId,
    DateTimeOffset CreatedAt)
{
    /// <summary>Builds the admin row from the aggregate (status/level/category as human-readable strings).</summary>
    public static GrammarLessonAdminDto FromDomain(GrammarLesson lesson) =>
        new(
            lesson.Id,
            lesson.Topic,
            lesson.Category.ToString(),
            lesson.Level.ToString(),
            lesson.Status.ToString(),
            lesson.Exercises.Count,
            lesson.VocabularyTopicId,
            lesson.CreatedAt);
}

/// <summary>
/// The payload for creating or updating a grammar lesson from the admin UI. <paramref name="Title"/>
/// maps to the lesson <see cref="GrammarLesson.Topic"/>; <paramref name="Category"/> is the
/// <see cref="Domain.Learning.ErrorCategory"/> name and <paramref name="Level"/> the CEFR band name.
/// </summary>
public sealed record GrammarLessonAdminUpsertDto(
    string Title,
    string Category,
    string Level);
