using Domain.Vocabulary;

namespace Application.Vocabulary.Dtos;

/// <summary>
/// The lowercase part-of-speech label the client shows as a word-class chip
/// ("noun"/"verb"/"adjective"/"phrasalverb"/…), or <c>null</c> when the word is untagged
/// (<see cref="PartOfSpeech.Other"/>). Matches the convention used by <c>TopicWordDto.PartOfSpeech</c>
/// so the client's existing Uzbek word-class labels apply unchanged.
/// </summary>
internal static class PartOfSpeechLabels
{
    public static string? Of(PartOfSpeech pos) =>
        pos == PartOfSpeech.Other ? null : pos.ToString().ToLowerInvariant();
}

/// <summary>A vocabulary item with its current SRS state (Vocabulary Review screen).</summary>
public sealed record VocabularyItemDto(
    Guid Id,
    Guid LearnerId,
    string Word,
    string Translation,
    string? ExampleSentence,
    VocabularySource Source,
    ReviewStage Stage,
    int FailCount,
    DateTimeOffset? NextReviewAt,
    Guid? SourceTopicId,
    string? PartOfSpeech)
{
    public static VocabularyItemDto FromDomain(VocabularyItem item) =>
        new(
            item.Id,
            item.LearnerId,
            item.Word,
            item.Translation,
            item.ExampleSentence,
            item.Source,
            item.Schedule.Stage,
            item.Schedule.FailCount,
            item.Schedule.NextReviewAt,
            item.SourceTopicId,
            PartOfSpeechLabels.Of(item.PartOfSpeech));
}

/// <summary>A due word plus the mini-test to present for it (PROJECT-SPEC B.1).</summary>
public sealed record DueReviewDto(
    Guid Id,
    string Word,
    string Translation,
    string? ExampleSentence,
    ReviewStage Stage,
    MiniTestType MiniTestType,
    Guid? SourceTopicId,
    string? PartOfSpeech,
    // Plausible English-word options for MiniTestType.ClozeChoice (the correct word plus a few
    // distractors from the learner's own vocabulary, shuffled). Null for every other mini-test
    // type - the server verifies the learner's picked/typed option against Word either way, this
    // is only what the multiple-choice UI renders (GetDueReviewsQueryHandler).
    IReadOnlyList<string>? Options = null)
{
    public static DueReviewDto FromDomain(VocabularyItem item, IReadOnlyList<string>? options = null) =>
        new(
            item.Id,
            item.Word,
            item.Translation,
            item.ExampleSentence,
            item.Schedule.Stage,
            item.NextMiniTestType(),
            item.SourceTopicId,
            PartOfSpeechLabels.Of(item.PartOfSpeech),
            options);
}

/// <summary>Outcome of submitting a review (PROJECT-SPEC B.1).</summary>
public sealed record ReviewResultDto(
    Guid Id,
    ReviewStage Stage,
    bool Mastered,
    int FailCount,
    DateTimeOffset? NextReviewAt,
    // Whether THIS submission passed (server-verified for ClozeChoice/WrittenUsage, self-rated
    // otherwise) - lets the client show immediate right/wrong feedback without inferring it from
    // FailCount deltas.
    bool Passed,
    // Set only when Passed is false and the mode was MiniTestType.WrittenUsage - a structured code
    // (never free LLM prose, docs/development-guide.md rule 11) the client resolves to a vetted Uzbek explanation.
    WordUsageReasonCode? ReasonCode = null)
{
    public static ReviewResultDto FromDomain(VocabularyItem item, bool passed, WordUsageReasonCode? reasonCode = null) =>
        new(
            item.Id,
            item.Schedule.Stage,
            item.Schedule.Stage == ReviewStage.Mastered,
            item.Schedule.FailCount,
            item.Schedule.NextReviewAt,
            passed,
            reasonCode);
}

/// <summary>Summary returned by the daily notification job.</summary>
public sealed record DispatchResultDto(int NotifiedLearners, int TotalDueItems);
