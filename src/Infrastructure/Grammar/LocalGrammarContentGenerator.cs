using Application.Grammar.Models;
using Application.Grammar.Ports;
using Domain.Assessment;
using Domain.Grammar;
using Domain.Learning;

namespace Infrastructure.Grammar;

/// <summary>
/// Deterministic local stand-in for <see cref="IGrammarContentGenerator"/>, used when no LLM key is
/// configured so the Grammar module runs and is testable offline (same gating pattern as
/// <c>LocalReadingContentGenerator</c>). It builds a complete 5-step lesson for a topic's grammar
/// focus, taught in that topic's context: a contextual intro, an English rule note, recognition +
/// active-use exercises (each with an English explanation) and a Speaking + Writing application
/// task. The content is generic but valid (passes every domain invariant); real topic-specific
/// teaching comes from <see cref="LlmGrammarContentGenerator"/> in production.
/// </summary>
public sealed class LocalGrammarContentGenerator : IGrammarContentGenerator
{
    /// <summary>Exercises per topic grammar set (PROJECT-SPEC G.2).</summary>
    private const int ExerciseCount = 10;

    public Task<GeneratedGrammarContent> GenerateAsync(
        string topicTitle, string grammarFocusCode, CefrLevel level,
        IReadOnlyList<string>? targetWords = null, CancellationToken cancellationToken = default)
    {
        var focus = Humanize(grammarFocusCode);
        var lower = topicTitle.ToLowerInvariant();

        var contextIntro =
            $"In this lesson about {lower}, we practise {focus}. " +
            $"Read these examples and notice how {focus} works when we talk about {lower}: " +
            $"first study the pattern, then try the exercises below.";

        var ruleExplanation =
            $"{Capitalize(focus)} is a grammar point you use often when speaking about {lower}. " +
            $"Look carefully at the example sentences, find the pattern, and use the same pattern " +
            $"in your own sentences about {lower}. At {level} this should feel natural with practice.";

        // A deterministic correct index per exercise so the answer is not always the first option.
        var seed = SeedFrom(grammarFocusCode + "|" + topicTitle);
        var rng = new Random(seed);

        // Ten exercises (PROJECT-SPEC G.2 - a topic's grammar set is 10 tasks). The three exercise
        // builders are cycled so the set keeps a mix of recognition and active-use, each variant
        // tagged with its number so prompts stay distinct.
        var exercises = new List<GeneratedGrammarExercise>(ExerciseCount);
        for (var i = 0; i < ExerciseCount; i++)
        {
            exercises.Add((i % 3) switch
            {
                0 => Recognition(focus, lower, rng, i + 1),
                1 => FillInBlank(focus, lower, rng, i + 1),
                _ => Rephrase(focus, lower, rng, i + 1),
            });
        }

        var examples = new List<GeneratedGrammarExample>
        {
            new($"We use {focus} when we talk about {lower}.", $"Biz {lower} haqida gapirob, {focus} ishlatamiz."),
            new($"She practises {focus} every day with {lower}.", $"U har kuni {lower} bilan {focus} ni mashq qiladi."),
            new($"They learned {focus} by reading about {lower}.", $"Ular {lower} o'qish orqali {focus} ni o'rgandilar."),
        };

        var commonMistakesUz = new List<string>
        {
            $"O'zbek tilida fe'lning zamonini ko'rsatmay qoldirish - inglizcha {focus} har doim zamon ko'rsatadi.",
            $"{focus} bilan otning ko'pligiga e'tibor bermaslik (he/she/it uchun -s qo'shimchasi).",
            $"So'rov gapda yordamchi fe'lning o'rnini adashtirib yuborish.",
        };

        var applicationTasks = new List<GeneratedGrammarApplicationTask>
        {
            new(SkillType.Speaking, $"Talk for one minute about {lower}, using {focus} at least three times."),
            new(SkillType.Writing, $"Write three sentences about {lower} that use {focus} correctly."),
        };

        return Task.FromResult(new GeneratedGrammarContent(
            contextIntro, ruleExplanation, examples, commonMistakesUz, exercises, applicationTasks));
    }

    private static GeneratedGrammarExercise Recognition(string focus, string topic, Random rng, int n)
    {
        var correct = $"This sentence about {topic} uses {focus} correctly.";
        var options = Shuffle(new List<string>
        {
            correct,
            $"This sentence about {topic} uses {focus} incorrectly.",
            $"This sentence about {topic} does not use {focus} at all.",
        }, correct, rng, out var correctIndex);

        return new GeneratedGrammarExercise(
            GrammarExerciseType.Recognition,
            $"Question {n}: which sentence uses {focus} correctly when talking about {topic}?",
            options, correctIndex,
            $"The correct sentence follows the {focus} pattern shown in the rule.");
    }

    private static GeneratedGrammarExercise FillInBlank(string focus, string topic, Random rng, int n)
    {
        var correct = "correct";
        var options = Shuffle(new List<string> { correct, "incorrect", "missing" }, correct, rng, out var correctIndex);

        return new GeneratedGrammarExercise(
            GrammarExerciseType.FillInBlank,
            $"Question {n}: complete the rule - to talk about {topic} we must use the ___ form of {focus}.",
            options, correctIndex,
            $"We always use the correct form of {focus} when we speak about {topic}.");
    }

    private static GeneratedGrammarExercise Rephrase(string focus, string topic, Random rng, int n)
    {
        var correct = $"a sentence that keeps {focus}";
        var options = Shuffle(new List<string>
        {
            correct,
            $"a sentence that drops {focus}",
            $"a sentence that changes the topic from {topic}",
        }, correct, rng, out var correctIndex);

        return new GeneratedGrammarExercise(
            GrammarExerciseType.Rephrase,
            $"Question {n}: choose the best way to rephrase a sentence about {topic} while keeping {focus}.",
            options, correctIndex,
            $"A good rephrasing keeps {focus} and still talks about {topic}.");
    }

    // Places the correct answer at a deterministic index so quizzes are reproducible across runs.
    private static IReadOnlyList<string> Shuffle(
        List<string> options, string correct, Random rng, out int correctIndex)
    {
        for (var i = options.Count - 1; i > 0; i--)
        {
            var j = rng.Next(i + 1);
            (options[i], options[j]) = (options[j], options[i]);
        }

        correctIndex = options.IndexOf(correct);
        return options;
    }

    private static string Humanize(string kebab) =>
        string.IsNullOrWhiteSpace(kebab) ? "this grammar point" : kebab.Trim().Replace('-', ' ');

    private static string Capitalize(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToUpperInvariant(text[0]) + text[1..];

    // Stable, cross-process hash (FNV-1a) so the generated quiz is deterministic per focus+topic.
    private static int SeedFrom(string value)
    {
        unchecked
        {
            const uint offset = 2166136261;
            const uint prime = 16777619;
            var hash = offset;
            foreach (var c in value)
            {
                hash ^= c;
                hash *= prime;
            }

            return (int)hash;
        }
    }
}
