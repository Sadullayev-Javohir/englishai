using Domain.Vocabulary;

namespace Application.Vocabulary.Admin;

/// <summary>
/// A vocabulary topic row for the admin catalog (PROJECT-SPEC module 4) - the curated metadata
/// plus computed display fields (level/status as strings, word count). Distinct from the learner
/// catalog <c>VocabularyTopicSummaryDto</c>, which layers mastery/gating/paywall on top.
/// </summary>
public sealed record VocabularyTopicAdminDto(
    Guid Id,
    string Slug,
    string Title,
    string TitleUz,
    string Category,
    string GrammarFocusCode,
    int Sequence,
    string Level,
    string Status,
    int WordCount,
    string Passage,
    IReadOnlyList<VocabularyTopicWordAdminDto> Words,
    DateTimeOffset CreatedAt)
{
    /// <summary>Builds the admin row from the aggregate (status/level as human-readable strings).</summary>
    public static VocabularyTopicAdminDto FromDomain(VocabularyTopic topic) =>
        new(
            topic.Id,
            topic.Slug,
            topic.Title,
            topic.TitleUz,
            topic.Category,
            topic.GrammarFocusCode,
            topic.Sequence,
            topic.Level.ToString(),
            topic.Status.ToString(),
            topic.Words.Count,
            topic.Passage,
            topic.Words.Select(VocabularyTopicWordAdminDto.FromDomain).ToArray(),
            topic.CreatedAt);
}

public sealed record VocabularyTopicWordAdminDto(
    string Word,
    string Translation,
    string? ExampleSentence,
    string PartOfSpeech,
    string LexicalCategory,
    string? Register,
    string? UsageNote,
    string? ImageUrl,
    string? ImageSource,
    string? ImageAttribution)
{
    public static VocabularyTopicWordAdminDto FromDomain(TopicWord word) =>
        new(word.Word, word.Translation, word.ExampleSentence, word.PartOfSpeech.ToString(),
            word.LexicalCategory.ToString(), word.Register, word.UsageNote,
            word.ImageUrl, word.ImageSource, word.ImageAttribution);
}
