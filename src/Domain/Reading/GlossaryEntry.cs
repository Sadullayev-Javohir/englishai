using Domain.Common;

namespace Domain.Reading;

/// <summary>
/// An interactive vocabulary entry attached to a reading passage (PROJECT-SPEC Faza 6 -
/// "darajalangan matn + interaktiv lug'at"). When the learner taps a glossed word in the
/// text, this vetted Uzbek <see cref="Translation"/> is shown and the word can be sent to
/// the SRS as a <see cref="Domain.Vocabulary.VocabularySource.Reading"/> item. The Uzbek
/// text is curated content, not free-generated at runtime (docs/development-guide.md rule 11).
/// </summary>
public sealed class GlossaryEntry
{
    // Parameterless ctor for EF Core materialization.
    private GlossaryEntry()
    {
        Word = null!;
        Translation = null!;
    }

    private GlossaryEntry(string word, string translation, string? exampleSentence)
    {
        Id = Guid.NewGuid();
        Word = word;
        Translation = translation;
        ExampleSentence = exampleSentence;
    }

    public Guid Id { get; private set; }

    /// <summary>The English word/phrase as it appears in the passage.</summary>
    public string Word { get; private set; }

    /// <summary>The vetted Uzbek translation shown when the word is tapped.</summary>
    public string Translation { get; private set; }

    /// <summary>Optional context sentence the word was drawn from.</summary>
    public string? ExampleSentence { get; private set; }

    public static GlossaryEntry Create(string word, string translation, string? exampleSentence = null)
    {
        if (string.IsNullOrWhiteSpace(word))
            throw new DomainException("Glossary word must not be empty.");
        if (string.IsNullOrWhiteSpace(translation))
            throw new DomainException("Glossary translation must not be empty.");

        return new GlossaryEntry(word.Trim(), translation.Trim(), exampleSentence?.Trim());
    }
}
