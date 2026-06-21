using System.Text.RegularExpressions;
using Application.Writing;
using Application.Writing.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Writing;

namespace Infrastructure.Writing;

/// <summary>
/// Deterministic, dependency-free writing assessor for dev/tests and for running the app
/// without an LLM key (the same config-gating pattern as the Speaking adapters, docs/development-guide.md
/// rules 10/15). It scores the four G.3 dimensions from real text features - length vs the
/// task's word range, sentence structure, lexical variety and a few detectable grammar
/// patterns - and emits structured issue codes (rule 11), never free-form Uzbek. The real
/// <see cref="HermesWritingAssessor"/> replaces it behind the same port when a key is set.
/// </summary>
public sealed partial class LocalWritingAssessor : ITopicWritingAssessor
{
    // A clear, deterministically detectable subject-verb agreement slip (e.g. "he go").
    [GeneratedRegex(@"\b(he|she|it)\s+(go|do|have|make|want|like|need|say|play|work)\b",
        RegexOptions.IgnoreCase)]
    private static partial Regex SubjectVerbSlip();

    private static readonly string[] LinkingWords =
    {
        "however", "because", "therefore", "for example", "in addition",
        "although", "moreover", "firstly", "finally", "on the other hand",
    };

    // A floor on the share of tokens that must be recognizable common English words for a
    // submission to be scored at all. Real writing - even an A1 learner's, full of mistakes -
    // is built mostly from everyday words (function words like the/is/and/to plus common
    // vocabulary), so its ratio is comfortably above this. Random keystrokes ("asdf qwer
    // zxcv") or non-English text fall far below it and must not be rewarded for "variety".
    private const double MinEnglishRatio = 0.20;

    [GeneratedRegex(@"[a-z']+")]
    private static partial Regex WordToken();

    public Task<WritingAssessment> AssessAsync(
        WritingTask task,
        string text,
        CefrLevel assessmentLevel,
        CancellationToken cancellationToken)
    {
        var words = WritingTask.CountWords(text);
        var lower = text.ToLowerInvariant();

        var issues = new List<WritingIssue>();

        // Genuine-English gate (runs before any reward logic): without it, gibberish scores
        // top marks for lexical variety (every random token is "unique") and grammar (no
        // detectable slips), inflating the placement level. A submission that does not read as
        // English scores the minimum on every dimension and carries a single explanatory code.
        var allTokens = WordToken().Matches(lower).Select(m => m.Value).ToList();
        var recognizedRatio = allTokens.Count == 0
            ? 0
            : (double)allTokens.Count(t => CommonEnglishWords.Contains(t)) / allTokens.Count;

        if (allTokens.Count == 0 || recognizedRatio < MinEnglishRatio)
        {
            issues.Add(new WritingIssue(
                WritingDimension.TaskAchievement, "writing.issue.not_english", 0, Math.Min(text.Length, 1), null));

            var floored = new[]
            {
                new DimensionScore(WritingDimension.TaskAchievement, WritingAssessment.MinDimensionScore),
                new DimensionScore(WritingDimension.Coherence, WritingAssessment.MinDimensionScore),
                new DimensionScore(WritingDimension.LexicalResource, WritingAssessment.MinDimensionScore),
                new DimensionScore(WritingDimension.GrammaticalAccuracy, WritingAssessment.MinDimensionScore),
            };
            return Task.FromResult(WritingAssessment.Create(
                task.Id, floored, issues, WritingAssessmentSource.LocalFallback));
        }

        // Task achievement is relative to the learner's assessment level. Browsing a harder topic
        // must not silently make an A1 learner satisfy A2/C2 production requirements.
        var (expectedMinWords, _) = WritingWordRange.For(assessmentLevel);
        int taskScore;
        if (words >= expectedMinWords)
        {
            taskScore = 5;
        }
        else if (words >= expectedMinWords / 2)
        {
            taskScore = 3;
            issues.Add(new WritingIssue(WritingDimension.TaskAchievement, "writing.issue.too_short", 0, Math.Min(text.Length, 1), null));
        }
        else
        {
            taskScore = 1;
            issues.Add(new WritingIssue(WritingDimension.TaskAchievement, "writing.issue.too_short", 0, Math.Min(text.Length, 1), null));
        }

        // Coherence expectations rise with CEFR. A1/A2 writing is allowed to consist of clear,
        // simple sentences; formal linking devices only become a grading requirement at B1+.
        var sentenceCount = Math.Max(1, text.Count(c => c is '.' or '!' or '?'));
        var hasLinking = LinkingWords.Any(w => lower.Contains(w));
        var coherenceScore = assessmentLevel switch
        {
            CefrLevel.A1 => sentenceCount >= 3 ? 5 : sentenceCount >= 2 ? 4 : 3,
            CefrLevel.A2 => sentenceCount >= 4 ? 5 : sentenceCount >= 2 ? 4 : 3,
            _ => sentenceCount switch
            {
                >= 4 => hasLinking ? 5 : 4,
                >= 2 => hasLinking ? 4 : 3,
                _ => 2,
            },
        };
        if (assessmentLevel >= CefrLevel.B1 && !hasLinking && words > 0)
            issues.Add(new WritingIssue(WritingDimension.Coherence, "writing.issue.weak_coherence", 0, Math.Min(text.Length, 1), null));

        // Lexical resource: ratio of distinct words to total (variety vs repetition).
        var tokens = allTokens;
        var uniqueRatio = tokens.Count == 0 ? 0 : (double)tokens.Distinct().Count() / tokens.Count;
        var lexicalScore = uniqueRatio switch
        {
            >= 0.6 => 5,
            >= 0.5 => 4,
            >= 0.4 => 3,
            >= 0.3 => 2,
            _ => 1,
        };

        // Grammatical accuracy: each detected subject-verb slip is one grammar issue feeding
        // the heatmap (Category set) and lowers the score.
        foreach (Match match in SubjectVerbSlip().Matches(text))
        {
            issues.Add(new WritingIssue(
                WritingDimension.GrammaticalAccuracy,
                "writing.issue.subject_verb_agreement",
                match.Index,
                match.Index + match.Length,
                ErrorCategory.SubjectVerbAgreement));
        }

        var grammarErrors = issues.Count(i => i.Dimension == WritingDimension.GrammaticalAccuracy);
        var grammarScore = Math.Max(1, 5 - grammarErrors);

        var scores = new[]
        {
            new DimensionScore(WritingDimension.TaskAchievement, taskScore),
            new DimensionScore(WritingDimension.Coherence, coherenceScore),
            new DimensionScore(WritingDimension.LexicalResource, lexicalScore),
            new DimensionScore(WritingDimension.GrammaticalAccuracy, grammarScore),
        };

        return Task.FromResult(WritingAssessment.Create(
            task.Id, scores, issues, WritingAssessmentSource.LocalFallback));
    }

    /// <summary>
    /// A compact set of the most frequent English words (function words plus everyday
    /// vocabulary). It is not a full dictionary - it only needs to be dense enough that any
    /// real English answer clears <see cref="MinEnglishRatio"/> while random keystrokes do
    /// not. Topic-specific or misspelled words a learner uses are expected to be absent; the
    /// ratio still stays high because connective everyday words dominate normal prose.
    /// </summary>
    private static readonly HashSet<string> CommonEnglishWords = new(StringComparer.Ordinal)
    {
        // articles, pronouns, determiners
        "a", "an", "the", "this", "that", "these", "those", "i", "you", "he", "she", "it",
        "we", "they", "me", "him", "her", "us", "them", "my", "your", "his", "its", "our",
        "their", "mine", "yours", "ours", "theirs", "myself", "yourself", "some", "any",
        "many", "much", "more", "most", "few", "all", "both", "each", "every", "other",
        "another", "such", "no", "none", "one", "two", "three", "first", "second", "next",
        // be / have / do / modals
        "am", "is", "are", "was", "were", "be", "been", "being", "have", "has", "had",
        "do", "does", "did", "will", "would", "shall", "should", "can", "could", "may",
        "might", "must", "ought", "need",
        // common verbs
        "go", "goes", "went", "gone", "going", "get", "got", "make", "makes", "made",
        "take", "took", "come", "came", "see", "saw", "seen", "know", "knew", "think",
        "thought", "say", "said", "tell", "told", "want", "wanted", "like", "liked",
        "love", "work", "works", "worked", "use", "used", "find", "found", "give", "gave",
        "feel", "felt", "become", "leave", "live", "lived", "learn", "study", "read",
        "write", "speak", "talk", "help", "play", "eat", "drink", "sleep", "buy", "sell",
        "start", "stop", "begin", "end", "try", "ask", "answer", "call", "look", "watch",
        "show", "let", "put", "keep", "mean", "seem", "turn", "move", "open", "close",
        // prepositions / conjunctions / adverbs
        "of", "to", "in", "on", "at", "by", "for", "with", "about", "against", "between",
        "into", "through", "during", "before", "after", "above", "below", "up", "down",
        "out", "off", "over", "under", "again", "then", "once", "here", "there", "when",
        "where", "why", "how", "and", "but", "or", "so", "if", "because", "as", "until",
        "while", "although", "though", "however", "therefore", "also", "too", "very",
        "just", "only", "even", "still", "now", "today", "always", "never", "often",
        "sometimes", "usually", "really", "well", "not", "yes", "from", "than", "what",
        "which", "who", "whom", "whose",
        // very common nouns / adjectives
        "time", "day", "year", "people", "person", "man", "woman", "child", "family",
        "friend", "home", "house", "school", "city", "country", "world", "life", "water",
        "food", "money", "book", "word", "thing", "way", "place", "name", "part", "number",
        "work", "job", "hand", "eye", "morning", "night", "week", "month", "good", "bad",
        "big", "small", "new", "old", "great", "high", "low", "long", "short", "happy",
        "important", "different", "same", "easy", "hard", "right", "wrong", "true", "nice",
        "best", "better", "beautiful", "clear", "varied", "paragraph", "score", "example",
    };
}
