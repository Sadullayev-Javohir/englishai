using Domain.Reading;

namespace Application.Reading.Admin;

/// <summary>
/// A reading passage row for the admin catalog - curated metadata plus computed display fields
/// (level/status as strings, question and word counts).
/// </summary>
public sealed record ReadingPassageAdminDto(
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
    public static ReadingPassageAdminDto FromDomain(ReadingPassage passage) =>
        new(
            passage.Id,
            passage.Title,
            passage.Topic,
            passage.Level.ToString(),
            passage.Status.ToString(),
            passage.Questions.Count,
            passage.WordCount,
            passage.VocabularyTopicId,
            passage.CreatedAt);
}

/// <summary>Create/update payload for a reading passage (metadata only).</summary>
public sealed record ReadingPassageAdminUpsertDto(
    string Title,
    string Topic,
    string Level);
