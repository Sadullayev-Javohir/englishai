using Domain.Assessment;

namespace Domain.Vocabulary;

/// <summary>
/// The lexical category of a <see cref="TopicWord"/>. A topic deliberately teaches a balanced,
/// level-appropriate mix across these categories rather than the noun-heavy set a passage yields by
/// default, so learners build words and chunks they can actually <i>use</i> when speaking - the core
/// mission (docs/development-guide.md). The set spans single-word parts of speech (noun, verb, ...) and multi-word
/// lexical chunks (phrasal verbs, idioms, collocations, fixed expressions) because real fluency is
/// built from chunks, not just isolated words. The numeric values 1-4 are stable so content stored
/// before the taxonomy was widened keeps its meaning. <see cref="Other"/> is the default for legacy
/// content (no category) and for words that fit none of the taught categories.
/// </summary>
public enum PartOfSpeech
{
    Other = 0,

    // Single-word parts of speech (values 1-4 are the original taxonomy - never renumber).
    Noun = 1,
    Verb = 2,
    Adjective = 3,
    Adverb = 4,
    Preposition = 5,
    Conjunction = 6,
    Pronoun = 7,

    /// <summary>Determiners and articles (the, a, some, this, every, ...).</summary>
    Determiner = 8,

    // Multi-word lexical chunks - the heart of usable fluency.
    /// <summary>Verb + particle whose meaning is non-literal (give up, look after, run out of).</summary>
    PhrasalVerb = 9,

    /// <summary>Words that habitually go together (make a decision, heavy rain, fast food).</summary>
    Collocation = 10,

    /// <summary>Figurative fixed phrases (break the ice, piece of cake, under the weather).</summary>
    Idiom = 11,

    /// <summary>Useful fixed/functional phrases and sentence frames (by the way, on the other hand).</summary>
    Expression = 12,
}

/// <summary>Parsing helpers for the LLM's free-text part-of-speech tag.</summary>
public static class PartOfSpeechParser
{
    /// <summary>
    /// Maps the model's category tag (e.g. "noun", "phrasal verb", "idiom", "adj") to a
    /// <see cref="PartOfSpeech"/>. Unknown or missing tags become <see cref="PartOfSpeech.Other"/> so
    /// generation never fails on an unexpected value (rules 8, 11).
    /// </summary>
    public static PartOfSpeech Parse(string? tag)
    {
        var t = tag?.Trim().ToLowerInvariant().Replace('_', ' ').Replace('-', ' ');
        return t switch
        {
            "noun" or "n" => PartOfSpeech.Noun,
            "verb" or "v" => PartOfSpeech.Verb,
            "adjective" or "adj" or "a" => PartOfSpeech.Adjective,
            "adverb" or "adv" or "r" => PartOfSpeech.Adverb,
            "preposition" or "prep" => PartOfSpeech.Preposition,
            "conjunction" or "conj" => PartOfSpeech.Conjunction,
            "pronoun" or "pron" => PartOfSpeech.Pronoun,
            "determiner" or "article" or "articles" or "det"
                or "determiner article" or "determiners articles" => PartOfSpeech.Determiner,
            "phrasal verb" or "phrasalverb" or "phrasal" or "phrv" => PartOfSpeech.PhrasalVerb,
            "collocation" or "colloc" => PartOfSpeech.Collocation,
            "idiom" or "idioms" => PartOfSpeech.Idiom,
            "expression" or "expressions" or "expr" or "phrase" or "chunk" => PartOfSpeech.Expression,
            _ => PartOfSpeech.Other,
        };
    }
}

/// <summary>
/// Which lexical categories a topic should teach at each CEFR level, and roughly how many words of
/// each. Beginners build the single-word core (nouns/verbs/adjectives/adverbs plus a few function
/// words); multi-word chunks (collocations, then phrasal verbs, then idioms/expressions) are layered
/// in as the level rises, because forcing idioms into an A1 topic would be unnatural (level-adaptive
/// decision, docs/development-guide.md rule 11). This shapes the generator prompt; it is content/teaching policy, so
/// it lives in the domain rather than being hard-coded inside an LLM adapter.
/// </summary>
public static class TopicWordPlan
{
    /// <summary>One target slice of a topic's word set: a category and how many words to teach of it.</summary>
    public readonly record struct CategoryQuota(PartOfSpeech Category, int Count);

    /// <summary>
    /// The level-appropriate category quotas for a topic of the given level, scaled to roughly
    /// <paramref name="totalWords"/> words. The quotas are a target the generator aims for, not a hard
    /// constraint; the sum is kept at/under the requested total.
    /// </summary>
    public static IReadOnlyList<CategoryQuota> For(CefrLevel level, int totalWords)
    {
        // Weighted templates per band. Multi-word chunks appear only from B1+, and only at the top do
        // idioms/expressions get real weight. Weights are normalized to the requested word count.
        (PartOfSpeech cat, double weight)[] weights = level switch
        {
            CefrLevel.A1 or CefrLevel.A2 => new[]
            {
                (PartOfSpeech.Verb, 4.0), (PartOfSpeech.Noun, 4.0), (PartOfSpeech.Adjective, 3.0),
                (PartOfSpeech.Adverb, 2.0), (PartOfSpeech.Preposition, 1.0), (PartOfSpeech.Pronoun, 0.5),
                (PartOfSpeech.Determiner, 0.5),
            },
            CefrLevel.B1 or CefrLevel.B2 => new[]
            {
                (PartOfSpeech.Verb, 3.0), (PartOfSpeech.Noun, 3.0), (PartOfSpeech.Adjective, 2.0),
                (PartOfSpeech.Adverb, 1.5), (PartOfSpeech.PhrasalVerb, 2.0), (PartOfSpeech.Collocation, 2.0),
                (PartOfSpeech.Preposition, 1.0), (PartOfSpeech.Conjunction, 0.5),
            },
            _ => new[] // C1 / C2
            {
                (PartOfSpeech.Verb, 2.5), (PartOfSpeech.Noun, 2.5), (PartOfSpeech.Adjective, 2.0),
                (PartOfSpeech.Adverb, 1.0), (PartOfSpeech.PhrasalVerb, 2.0), (PartOfSpeech.Collocation, 2.0),
                (PartOfSpeech.Idiom, 1.5), (PartOfSpeech.Expression, 1.5),
            },
        };

        var totalWeight = weights.Sum(w => w.weight);
        var quotas = new List<CategoryQuota>(weights.Length);
        var allocated = 0;
        foreach (var (cat, weight) in weights.OrderByDescending(w => w.weight))
        {
            var count = (int)Math.Round(weight / totalWeight * totalWords, MidpointRounding.AwayFromZero);
            if (count <= 0)
                continue;
            if (allocated + count > totalWords)
                count = totalWords - allocated;
            if (count <= 0)
                continue;
            quotas.Add(new CategoryQuota(cat, count));
            allocated += count;
        }

        return quotas;
    }
}
