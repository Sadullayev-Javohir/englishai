using Application.Speaking.Dtos;
using Domain.Assessment;
using Domain.Speaking;
using Domain.Subscription;
using Domain.Vocabulary;

namespace Application.Vocabulary.Dtos;

/// <summary>
/// A catalog row for the topic list (no passage/words). <paramref name="Learned"/> is true once the
/// learner has practiced speaking about the topic for 5+ minutes (PROJECT-SPEC module 4 ↔ Faza 1).
/// </summary>
public sealed record VocabularyTopicSummaryDto(
    Guid Id,
    string Title,
    string TitleUz,
    string Category,
    CefrLevel Level,
    bool IsFilled,
    bool Learned,
    bool IsStarted,
    int PassedModuleCount,
    int RequiredModuleCount,
    bool IsMastered,
    bool IsLocked,
    bool RequiresPro,
    IReadOnlyList<TopicModuleScoreDto> Modules)
{
    /// <summary>Catalog row without mastery progress (defaults: 0 of six modules passed, unlocked).</summary>
    public static VocabularyTopicSummaryDto FromDomain(VocabularyTopic topic, bool learned) =>
        FromDomain(topic, learned, completion: null);

    /// <summary>
    /// Catalog row carrying the learner's six-module mastery progress for the topic (PROJECT-SPEC
    /// K.5) so the roadmap can show how far through a topic the learner is in a single page load.
    /// <see cref="Modules"/> lists every required skill's passed/unlocked state so the roadmap can
    /// show a per-skill checklist for each topic without a request per topic.
    /// <see cref="IsLocked"/> defaults to false; call <see cref="ApplySequentialLock"/> on the
    /// ordered list to set it.
    /// </summary>
    public static VocabularyTopicSummaryDto FromDomain(
        VocabularyTopic topic, bool learned, TopicCompletionRecord? completion) =>
        new(
            topic.Id,
            topic.Title,
            topic.TitleUz,
            topic.Category,
            topic.Level,
            topic.IsFilled,
            learned,
            completion is not null,
            completion?.PassedModuleCount ?? 0,
            TopicCompletionRecord.RequiredModules.Count,
            completion?.IsMastered ?? false,
            IsLocked: false,
            RequiresPro: false,
            ModuleStatuses(completion));

    /// <summary>
    /// The per-skill checklist (K.5) for the topic in the canonical learning order. Every skill is
    /// selectable while the topic itself is inside the learner's open topic window; passing still
    /// requires the mastery threshold and the canonical order remains the recommendation order.
    /// </summary>
    private static IReadOnlyList<TopicModuleScoreDto> ModuleStatuses(TopicCompletionRecord? completion) =>
        TopicCompletionRecord.RequiredModules
            .Select((module, index) => new TopicModuleScoreDto(
                module.ToString(),
                completion?.ScoreFor(module) ?? 0,
                completion?.IsModulePassed(module) ?? false,
                Unlocked: true,
                AchievedAt: completion?.ModuleScores.FirstOrDefault(m => m.Module == module)?.AchievedAt))
            .ToList();

    /// <summary>
    /// Applies topic-window gating (K.5): within a level's ordered topic list, all mastered topics and
    /// the next three unmastered topics are open. Each newly mastered topic advances the window by one
    /// topic. The input must already be in the level's learning order (Sequence).
    /// </summary>
    public static IReadOnlyList<VocabularyTopicSummaryDto> ApplySequentialLock(
        IReadOnlyList<VocabularyTopicSummaryDto> orderedTopics, bool fullAccess = false)
    {
        // Comped accounts (IComplimentaryAccess) bypass K.5 entirely: every topic - and every
        // skill module within it - is open, so they can jump to any section in any order.
        if (fullAccess)
            return orderedTopics.Select(t => t.Unlocked()).ToList();

        const int openUnmasteredTopicCount = 3;
        var result = new List<VocabularyTopicSummaryDto>(orderedTopics.Count);
        var openUnmasteredTopics = 0;
        foreach (var topic in orderedTopics)
        {
            var isOpen = topic.IsMastered || openUnmasteredTopics < openUnmasteredTopicCount;
            result.Add(isOpen ? topic.Unlocked() : topic.Locked());
            if (!topic.IsMastered)
                openUnmasteredTopics++;
        }

        return result;
    }

    /// <summary>
    /// Applies the trial paywall (PROJECT-SPEC H.1): a free learner may engage only the first
    /// <see cref="FreeTopicAccessPolicy.FreeTopicAllowance"/> topics they have started; later topics
    /// are flagged <see cref="RequiresPro"/> so the roadmap shows an upgrade badge. Privileged learners
    /// (active Premium or a comped account) never see the flag. Uses the pure
    /// <see cref="FreeTopicAccessPolicy"/> so the badge matches the server-side enforcement exactly.
    /// </summary>
    /// <param name="hasFullAccess">True for an active Premium subscription or a comped account.</param>
    /// <param name="startedTopicIdsEarliestFirst">
    /// Every topic the learner has started, oldest-first, across all levels (the free set is global).
    /// </param>
    public static IReadOnlyList<VocabularyTopicSummaryDto> ApplyTrialPaywall(
        IReadOnlyList<VocabularyTopicSummaryDto> topics,
        bool hasFullAccess,
        IReadOnlyList<Guid> startedTopicIdsEarliestFirst)
    {
        if (hasFullAccess)
            return topics;

        return topics
            .Select(t => t with
            {
                RequiresPro = !FreeTopicAccessPolicy
                    .Evaluate(hasFullAccess: false, startedTopicIdsEarliestFirst, t.Id).IsAllowed,
            })
            .ToList();
    }

    /// <summary>Returns this row with every lock cleared: the topic and all its modules unlocked.</summary>
    private VocabularyTopicSummaryDto Unlocked() => this with
    {
        IsLocked = false,
        RequiresPro = false,
        Modules = Modules.Select(m => m with { Unlocked = true }).ToList(),
    };

    /// <summary>Returns this row with the topic and every skill module locked.</summary>
    private VocabularyTopicSummaryDto Locked() => this with
    {
        IsLocked = true,
        Modules = Modules.Select(m => m with { Unlocked = false }).ToList(),
    };
}

/// <summary>
/// A target word as shown in the reader: English word, vetted Uzbek meaning, the IPA used by
/// the hover tooltip's pronunciation, an example sentence from the passage, and the lexical category
/// (lowercased enum name: "noun"/"verb"/"phrasalverb"/"idiom"/...; null when untagged) the client
/// groups the words by and shows as a chip.
/// </summary>
public sealed record TopicWordDto(
    string Word,
    string Translation,
    string? Ipa,
    string? ExampleSentence,
    string? PartOfSpeech,
    string LexicalCategory,
    string? Register,
    string? UsageNote,
    string? ImageUrl,
    string? ImageAttribution);

/// <summary>
/// A topic's target word for the OTHER skills to highlight in their own passage/transcript/prompt:
/// the English word (or multi-word chunk), its vetted Uzbek meaning and an example. This is the same
/// canonical set the Vocabulary reader teaches - one PostgreSQL source (<c>VocabularyTopic.Words</c>)
/// reused everywhere so a learner meets each word, highlighted identically, across every skill.
/// </summary>
public sealed record TargetWordDto(string Word, string Translation, string? ExampleSentence);

/// <summary>
/// A quiz question as shown to the learner - the correct answer is intentionally omitted so
/// grading happens server-side (PROJECT-SPEC B.1 "Matnda tanlash").
/// </summary>
public sealed record TopicQuizQuestionDto(int Index, string Prompt, IReadOnlyList<string> Options)
{
    public static TopicQuizQuestionDto FromDomain(TopicQuizQuestion q) => new(q.Index, q.Prompt, q.Options);
}

/// <summary>
/// Full topic detail: the passage, its target words (with IPA), and the practice quiz.
/// <see cref="IsReady"/> is false while the passage is still being generated (honest pending).
/// </summary>
public sealed record VocabularyTopicDetailDto(
    Guid Id,
    string Title,
    string TitleUz,
    string Category,
    CefrLevel Level,
    bool IsReady,
    string Passage,
    IReadOnlyList<TopicWordDto> Words,
    IReadOnlyList<TopicQuizQuestionDto> Quiz);

/// <summary>
/// One sentence of the topic passage paired with its Uzbek translation, in original reading order.
/// Backs the "MATN" card's sentence-by-sentence translation toggle.
/// </summary>
public sealed record PassageSentenceDto(string English, string? Uzbek);

/// <summary>
/// The passage split into sentences, each with its Uzbek translation (when available). Returned by
/// <c>POST /api/vocabulary/topic/{topicId}/passage-translation</c> so the client can show the text
/// then reveal "sentence → its translation → next sentence → its translation" in order.
/// </summary>
public sealed record VocabularyTopicPassageTranslationDto(IReadOnlyList<PassageSentenceDto> Sentences);

/// <summary>The graded outcome of one quiz question, with the correct answer revealed.</summary>
public sealed record TopicQuizOutcomeDto(
    int QuestionIndex,
    string Word,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect)
{
    public static TopicQuizOutcomeDto FromDomain(TopicQuizOutcome o) =>
        new(o.QuestionIndex, o.Word, o.SelectedOptionIndex, o.CorrectOptionIndex, o.IsCorrect);
}

/// <summary>The result of submitting a topic quiz (PROJECT-SPEC B.1 retrieval practice).</summary>
public sealed record TopicQuizResultDto(
    Guid TopicId,
    int TotalQuestions,
    int CorrectCount,
    int ScorePercent,
    IReadOnlyList<TopicQuizOutcomeDto> Outcomes,
    TopicCompletionDto? Completion = null)
{
    public static TopicQuizResultDto FromDomain(TopicQuizResult result) =>
        new(result.TopicId, result.TotalQuestions, result.CorrectCount, result.ScorePercent,
            result.Outcomes.Select(TopicQuizOutcomeDto.FromDomain).ToList());
}

/// <summary>
/// The outcome of checking a learner's spoken pronunciation of a single target word
/// (PROJECT-SPEC B.1 "Og'zaki ishlatish" / module 4): whether it was said correctly, the
/// scores, and a vetted Uzbek feedback line (rule 11).
/// </summary>
public sealed record WordPronunciationCheckDto(
    string Word,
    bool Recognized,
    bool Correct,
    bool IsAuthentic,
    double OverallScore,
    double AccuracyScore,
    PronunciationErrorType ErrorType,
    IReadOnlyList<PhonemePronunciationDto> Phonemes,
    string? FeedbackUz);
