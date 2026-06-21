using Application.Vocabulary.Dtos;
using Domain.Vocabulary;

namespace Application.Vocabulary;

/// <summary>
/// Extracts the English target words a <see cref="VocabularyTopic"/> teaches, for the other skills to
/// reuse (the unified topic spine - vocabulary is the spine root, every other skill weaves the same
/// words in so the learner meets each word repeatedly across skills, in context). Returns an empty
/// list when the topic has not been filled yet, in which case the skill generates as before.
/// </summary>
public static class TopicTargetWords
{
    public static IReadOnlyList<string> Of(VocabularyTopic topic) =>
        topic.Words.Count == 0
            ? Array.Empty<string>()
            : topic.Words.Select(w => w.Word).ToList();

    /// <summary>
    /// The topic's target words with their vetted Uzbek meaning and example, so the other skills can
    /// highlight the SAME canonical set in their own text (one PostgreSQL source of truth). Empty when
    /// the topic has not been filled yet.
    /// </summary>
    public static IReadOnlyList<TargetWordDto> DetailedOf(VocabularyTopic topic) =>
        topic.Words.Count == 0
            ? Array.Empty<TargetWordDto>()
            : topic.Words.Select(w => new TargetWordDto(w.Word, w.Translation, w.ExampleSentence)).ToList();
}
