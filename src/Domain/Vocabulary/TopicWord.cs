using Domain.Common;

namespace Domain.Vocabulary;

/// <summary>
/// One target word taught by a <see cref="VocabularyTopic"/>: the English word, its vetted
/// Uzbek meaning, and an example sentence that uses the word in the passage's context. The
/// word's IPA/pronunciation is not stored here - it is derived at read time from the
/// pronunciation dictionary (content store) so it stays in one place. The Uzbek
/// <see cref="Translation"/> is the sanctioned dynamic-Uzbek path (translating a source
/// word, not free-inventing UI copy) per docs/development-guide.md rule 11.
/// </summary>
public sealed class TopicWord
{
    // Parameterless ctor for EF Core materialization.
    private TopicWord()
    {
        Word = null!;
        Translation = null!;
    }

    private TopicWord(
        string word, string translation, string? exampleSentence,
        PartOfSpeech partOfSpeech, LexicalCategory lexicalCategory, string? register,
        string? usageNote, string? imageUrl = null, string? imageSource = null,
        string? imageAttribution = null)
    {
        Word = word;
        Translation = translation;
        ExampleSentence = exampleSentence;
        PartOfSpeech = partOfSpeech;
        LexicalCategory = lexicalCategory;
        Register = register;
        UsageNote = usageNote;
        ImageUrl = imageUrl;
        ImageSource = imageSource;
        ImageAttribution = imageAttribution;
    }

    /// <summary>The English word/phrase the learner meets in the passage.</summary>
    public string Word { get; private set; }

    /// <summary>The vetted Uzbek meaning shown on hover/tap.</summary>
    public string Translation { get; private set; }

    /// <summary>An example sentence (from the passage) that uses the word; drives the cloze quiz.</summary>
    public string? ExampleSentence { get; private set; }

    /// <summary>
    /// The word's grammatical category. Topics are generated with a balanced spread of these so a
    /// learner builds usable verbs/adjectives/adverbs, not just nouns (see <see cref="PartOfSpeech"/>).
    /// </summary>
    public PartOfSpeech PartOfSpeech { get; private set; }

    public LexicalCategory LexicalCategory { get; private set; } = LexicalCategory.PartsOfSpeech;

    public string? Register { get; private set; }

    public string? UsageNote { get; private set; }

    /// <summary>
    /// A licensed/derived illustration URL for the word itself (NOT the topic cover). Backfilled once
    /// from a licensed image provider (Unsplash/Wikimedia, rule 12) and stored so every learner and
    /// every skill that teaches the word reuses the same picture. Null until backfilled - the UI falls
    /// back to a live keyword image search when absent (rule 12 - a missing image is never a blank card).
    /// </summary>
    public string? ImageUrl { get; private set; }

    /// <summary>The provider the word image came from (e.g. "Unsplash"), kept for provenance.</summary>
    public string? ImageSource { get; private set; }

    /// <summary>Human-readable attribution for the word image (rule 12), or null.</summary>
    public string? ImageAttribution { get; private set; }

    public static TopicWord Create(
        string word, string translation, string? exampleSentence = null,
        PartOfSpeech partOfSpeech = PartOfSpeech.Other,
        LexicalCategory lexicalCategory = LexicalCategory.PartsOfSpeech,
        string? register = null, string? usageNote = null,
        string? imageUrl = null, string? imageSource = null, string? imageAttribution = null)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new DomainException("Topic word must not be empty.");
        if (string.IsNullOrWhiteSpace(translation))
            throw new DomainException("Topic word translation must not be empty.");

        return new TopicWord(
            word.Trim(),
            translation.Trim(),
            string.IsNullOrWhiteSpace(exampleSentence) ? null : exampleSentence.Trim(),
            partOfSpeech,
            lexicalCategory,
            string.IsNullOrWhiteSpace(register) ? null : register.Trim(),
            string.IsNullOrWhiteSpace(usageNote) ? null : usageNote.Trim(),
            string.IsNullOrWhiteSpace(imageUrl) ? null : imageUrl.Trim(),
            string.IsNullOrWhiteSpace(imageSource) ? null : imageSource.Trim(),
            string.IsNullOrWhiteSpace(imageAttribution) ? null : imageAttribution.Trim());
    }

    /// <summary>
    /// Persists the backfilled word illustration. Only mutates when a usable URL is supplied, so a
    /// failed/empty backfill pass leaves an already-stored image (or null) untouched. Returns true when
    /// the stored value actually changed, so the caller knows whether to persist the aggregate.
    /// </summary>
    public bool SetImage(string? imageUrl, string? imageSource, string? imageAttribution)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
            return false;

        var url = imageUrl.Trim();
        var source = string.IsNullOrWhiteSpace(imageSource) ? null : imageSource.Trim();
        var attribution = string.IsNullOrWhiteSpace(imageAttribution) ? null : imageAttribution.Trim();
        if (string.Equals(ImageUrl, url, StringComparison.OrdinalIgnoreCase)
            && string.Equals(ImageSource, source, StringComparison.Ordinal)
            && string.Equals(ImageAttribution, attribution, StringComparison.Ordinal))
            return false;

        ImageUrl = url;
        ImageSource = source;
        ImageAttribution = attribution;
        return true;
    }
}
