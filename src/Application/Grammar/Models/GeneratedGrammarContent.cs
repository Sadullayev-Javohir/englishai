using Domain.Grammar;
using Domain.Learning;

namespace Application.Grammar.Models;

/// <summary>
/// One generated grammar exercise: its step type (recognition / fill-in-blank / rephrase), an
/// English prompt, answer options, the index of the correct option and a short English explanation
/// of why it is correct (the immersion teaching note).
/// </summary>
public sealed record GeneratedGrammarExercise(
    GrammarExerciseType Type,
    string Prompt,
    IReadOnlyList<string> Options,
    int CorrectOptionIndex,
    string? Explanation);

/// <summary>One generated example sentence: the exact English and its short Uzbek meaning.</summary>
public sealed record GeneratedGrammarExample(string English, string Uzbek);

/// <summary>One generated step-5 application task: the module to practise in and an English prompt.</summary>
public sealed record GeneratedGrammarApplicationTask(SkillType TargetSkill, string Prompt);

/// <summary>
/// The LLM-generated grammar lesson for a learning-spine topic's grammar focus, in that topic's own
/// context: a contextual English intro (step 1), an English rule explanation (step 2), example
/// sentences, the common mistakes Uzbek learners make, recognition + active-use exercises (steps 3-4)
/// and Speaking/Writing application tasks (step 5). Produced by an <see cref="Ports.IGrammarContentGenerator"/>
/// and cached on the lesson (rules 8, 10). Everything is the target language (English teaching
/// content) - the sanctioned dynamic path of rule 11.
/// </summary>
public sealed record GeneratedGrammarContent(
    string ContextIntro,
    string RuleExplanation,
    IReadOnlyList<GeneratedGrammarExample> Examples,
    IReadOnlyList<string> CommonMistakesUz,
    IReadOnlyList<GeneratedGrammarExercise> Exercises,
    IReadOnlyList<GeneratedGrammarApplicationTask> ApplicationTasks)
{
    /// <summary>An empty result, signalling generation was unavailable (lesson stays pending).</summary>
    public static GeneratedGrammarContent Empty { get; } = new(
        string.Empty,
        string.Empty,
        Array.Empty<GeneratedGrammarExample>(),
        Array.Empty<string>(),
        Array.Empty<GeneratedGrammarExercise>(),
        Array.Empty<GeneratedGrammarApplicationTask>());

    /// <summary>True when there is a usable context intro, rule explanation and at least one exercise.</summary>
    public bool HasContent =>
        !string.IsNullOrWhiteSpace(ContextIntro) &&
        !string.IsNullOrWhiteSpace(RuleExplanation) &&
        Exercises.Count > 0;
}
