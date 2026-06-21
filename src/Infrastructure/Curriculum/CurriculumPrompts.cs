using Domain.Assessment;
using Domain.Vocabulary;

namespace Infrastructure.Curriculum;

internal static class CurriculumPrompts
{
    public const string Version = "v4.1";
    private const string LanguageRule = "Use only English and Latin-script Uzbek. Never use Cyrillic, CJK, Arabic, accented third-language letters, markdown fences, or prose outside JSON. Uzbek must use correct natural words and ASCII apostrophes o' and g'.";

    public static (string System, string User) Vocabulary(VocabularyTopic topic) =>
        ("You design a rigorous English curriculum for Uzbek learners. Return JSON only. " + LanguageRule,
        $"""
        TOPIC: {topic.Title}
        CEFR: {topic.Level}
        Create exactly 20 unique, strongly topic-related lexical items and one English passage using all 20 naturally.
        Return JSON with passage and words. Each word object must contain w, uz, ex, category, pos, register, and usageNote.
        category must be one of: PartsOfSpeech, PhrasalVerb, Idiom, Collocation, Expression, Proverb, Slang, FormalExpression, InformalExpression, FixedPhrase, SentenceStarter, LinkingWord, DiscourseMarker, CommonQuestion, CommonResponse, Greeting, Farewell, PoliteExpression.
        Every English item must appear in passage. Uzbek meanings must be accurate and natural.
        """);

    public static (string System, string User) Grammar(VocabularyTopic topic, IReadOnlyList<string> words, string? reviewFocus) =>
        ("Return one JSON object only. Required exact top-level keys: intro, rule, examples, exercises, tasks. " + LanguageRule,
        $"""
        TOPIC: {topic.Title}
        CEFR: {topic.Level}
        PRIMARY GRAMMAR: {topic.GrammarFocusCode}
        REVIEW GRAMMAR: {reviewFocus ?? "none"}
        VOCABULARY: {string.Join(", ", words)}
        The JSON object must have exactly this schema:
        intro: non-empty English string.
        rule: non-empty English explanation string.
        examples: array of 4 to 6 objects, each with en and natural Latin Uzbek uz strings.
        exercises: array of exactly 10 objects. Every object must contain type, q, options, answer, why. q and why are English strings. options must contain exactly 4 English strings. answer must be an integer from 0 to 3.
        tasks: array of exactly 2 objects, each with skill and prompt. skill is speaking or writing.
        Use at least 8 vocabulary items. Do not rename any key. Do not return lesson, content, grammar, data, sections, or nested wrapper objects.
        """);

    public static (string System, string User) Reading(VocabularyTopic topic, IReadOnlyList<string> words, string? reviewFocus) =>
        ("Create a CEFR reading lesson. JSON only. " + LanguageRule,
        $"""
        TOPIC: {topic.Title}; CEFR: {topic.Level}; PRIMARY GRAMMAR: {topic.GrammarFocusCode}; REVIEW GRAMMAR: {reviewFocus ?? "none"}; WORDS: {string.Join(", ", words)}.
        Return JSON with body, glossary, and questions. Each glossary item contains w, uz, and ex. Each question contains q, four options, zero-based answer, and why.
        Use at least 12 vocabulary items and both grammar focuses naturally. Exactly 6 questions.
        """);

    public static (string System, string User) Listening(VocabularyTopic topic, IReadOnlyList<string> words, string? reviewFocus) =>
        ("Return one JSON object only with exact keys transcript and questions. " + LanguageRule,
        $"""
        TOPIC: {topic.Title}; CEFR: {topic.Level}; PRIMARY GRAMMAR: {topic.GrammarFocusCode}; REVIEW GRAMMAR: {reviewFocus ?? "none"}; WORDS: {string.Join(", ", words)}.
        transcript must be a non-empty English spoken script. questions must be an array of exactly 6 objects. Every question object must contain q, options, answer, and why. options must contain exactly 4 strings and answer must be 0 to 3. Do not rename transcript or wrap the object.
        Use at least 10 vocabulary items and both grammar focuses naturally. Exactly 6 questions.
        """);

    public static (string System, string User) Writing(VocabularyTopic topic, IReadOnlyList<string> words, string? reviewFocus) =>
        ("Create one practical English writing task. JSON only. " + LanguageRule,
        $"""
        TOPIC: {topic.Title}; CEFR: {topic.Level}; PRIMARY GRAMMAR: {topic.GrammarFocusCode}; REVIEW GRAMMAR: {reviewFocus ?? "none"}; WORDS: {string.Join(", ", words)}.
        Return JSON with prompt, guidance, and requiredWords. The prompt must require primary grammar and at least 8 listed words. Provide 3-5 English guidance items.
        """);

    public static (string System, string User) Speaking(VocabularyTopic topic, IReadOnlyList<string> words, string? reviewFocus) =>
        ("Return one JSON object only. Exact required keys: objective, priorityWords, questions, followUps, modelAnswerOutline, rubric. " + LanguageRule,
        $"""
        TOPIC: {topic.Title}; CEFR: {topic.Level}; PRIMARY GRAMMAR: {topic.GrammarFocusCode}; REVIEW GRAMMAR: {reviewFocus ?? "none"}; WORDS: {string.Join(", ", words)}.
        objective must be a non-empty English string. priorityWords must contain exactly 10 items copied from WORDS. questions must contain exactly 6 English questions. followUps and modelAnswerOutline must be non-empty arrays. rubric must be an object containing non-empty lexicalCoverage, grammarAccuracy, fluency, and pronunciation strings. Do not rename objective. Do not return a wrapper such as lesson, content, blueprint, data, or speaking.
        """);

    public static (string System, string User) Book(string title, string synopsis, string sectionTitle,
        int sectionNumber, int totalSections, CefrLevel level) =>
        ("Return one JSON object only with exact keys body and questions. " + LanguageRule,
        $"""
        BOOK: {title}; SYNOPSIS: {synopsis}; SECTION: {sectionTitle}; SECTION {sectionNumber}/{totalSections}; CEFR: {level}.
        body must be a non-empty coherent English story section. questions must contain exactly 10 answerable objects. Every question contains q, exactly 4 options, answer from 0 to 3, and why. Do not rename body or wrap the object.
        """);

    public static string? ReviewFocus(VocabularyTopic topic, IReadOnlyList<VocabularyTopic> levelTopics)
    {
        return levelTopics
            .Where(x => x.Sequence < topic.Sequence)
            .OrderByDescending(x => x.Sequence)
            .Select(x => x.GrammarFocusCode)
            .FirstOrDefault(x => !string.Equals(x, topic.GrammarFocusCode, StringComparison.OrdinalIgnoreCase));
    }
}
