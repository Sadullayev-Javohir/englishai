namespace Domain.Vocabulary;

/// <summary>The curriculum-facing lexical category taught by one topic item.</summary>
public enum LexicalCategory
{
    PartsOfSpeech = 1,
    PhrasalVerb = 2,
    Idiom = 3,
    Collocation = 4,
    Expression = 5,
    Proverb = 6,
    Slang = 7,
    FormalExpression = 8,
    InformalExpression = 9,
    FixedPhrase = 10,
    SentenceStarter = 11,
    LinkingWord = 12,
    DiscourseMarker = 13,
    CommonQuestion = 14,
    CommonResponse = 15,
    Greeting = 16,
    Farewell = 17,
    PoliteExpression = 18,
}

public static class LexicalCategoryParser
{
    public static LexicalCategory Parse(string? value, PartOfSpeech partOfSpeech = PartOfSpeech.Other)
    {
        var normalized = value?.Trim().ToLowerInvariant().Replace("_", " ").Replace("-", " ");
        return normalized switch
        {
            "parts of speech" or "part of speech" or "word" => LexicalCategory.PartsOfSpeech,
            "phrasal verb" or "phrasal verbs" => LexicalCategory.PhrasalVerb,
            "idiom" or "idioms" => LexicalCategory.Idiom,
            "collocation" or "collocations" => LexicalCategory.Collocation,
            "expression" or "expressions" => LexicalCategory.Expression,
            "proverb" or "proverbs" => LexicalCategory.Proverb,
            "slang" => LexicalCategory.Slang,
            "formal expression" or "formal expressions" => LexicalCategory.FormalExpression,
            "informal expression" or "informal expressions" => LexicalCategory.InformalExpression,
            "fixed phrase" or "fixed phrases" => LexicalCategory.FixedPhrase,
            "sentence starter" or "sentence starters" => LexicalCategory.SentenceStarter,
            "linking word" or "linking words" => LexicalCategory.LinkingWord,
            "discourse marker" or "discourse markers" => LexicalCategory.DiscourseMarker,
            "common question" or "common questions" => LexicalCategory.CommonQuestion,
            "common response" or "common responses" => LexicalCategory.CommonResponse,
            "greeting" or "greetings" => LexicalCategory.Greeting,
            "farewell" or "farewells" => LexicalCategory.Farewell,
            "polite expression" or "polite expressions" => LexicalCategory.PoliteExpression,
            _ => partOfSpeech switch
            {
                PartOfSpeech.PhrasalVerb => LexicalCategory.PhrasalVerb,
                PartOfSpeech.Idiom => LexicalCategory.Idiom,
                PartOfSpeech.Collocation => LexicalCategory.Collocation,
                PartOfSpeech.Expression => LexicalCategory.Expression,
                _ => LexicalCategory.PartsOfSpeech,
            },
        };
    }
}
