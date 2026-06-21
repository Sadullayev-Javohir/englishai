using Domain.Common;

namespace Domain.Grammar;

/// <summary>
/// A single multiple-choice grammar exercise (PROJECT-SPEC G.2 steps 3-4). The English
/// prompt and options are curated content; the optional <see cref="HintCode"/> resolves to
/// a vetted Uzbek hint through the content layer (docs/development-guide.md rule 11) rather than being
/// free-generated. Grading is server-side, so the correct answer never leaves the domain
/// until a submission is graded.
/// </summary>
public sealed class GrammarExercise
{
    public const int MinOptions = 2;

    // Parameterless ctor for EF Core materialization.
    private GrammarExercise()
    {
        Prompt = null!;
        Options = null!;
    }

    private GrammarExercise(
        GrammarExerciseType type, string prompt, IReadOnlyList<string> options, int correctOptionIndex,
        string? hintCode, string? explanation)
    {
        Id = Guid.NewGuid();
        Type = type;
        Prompt = prompt;
        Options = options;
        CorrectOptionIndex = correctOptionIndex;
        HintCode = hintCode;
        Explanation = explanation;
    }

    public Guid Id { get; private set; }

    /// <summary>Which step of the lesson this exercise belongs to (recognition / active use).</summary>
    public GrammarExerciseType Type { get; private set; }

    /// <summary>The English exercise prompt shown to the learner.</summary>
    public string Prompt { get; private set; }

    /// <summary>The answer choices, in display order.</summary>
    public IReadOnlyList<string> Options { get; private set; }

    /// <summary>Index into <see cref="Options"/> of the correct answer.</summary>
    public int CorrectOptionIndex { get; private set; }

    /// <summary>Optional content-store code for a vetted Uzbek hint (rule 11, legacy path).</summary>
    public string? HintCode { get; private set; }

    /// <summary>
    /// Optional generated English explanation of why the answer is correct (the immersion teaching
    /// note on the topic-scoped path, mirroring a reading question's explanation). Shown only for a
    /// wrong answer.
    /// </summary>
    public string? Explanation { get; private set; }

    public static GrammarExercise Create(
        GrammarExerciseType type, string prompt, IReadOnlyList<string> options,
        int correctOptionIndex, string? hintCode = null, string? explanation = null)
    {
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Exercise prompt must not be empty.");
        if (options is null || options.Count < MinOptions)
            throw new DomainException($"An exercise needs at least {MinOptions} options.");
        if (options.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Exercise options must not be empty.");
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new DomainException("Correct option index is out of range.");

        return new GrammarExercise(
            type,
            prompt.Trim(),
            options.Select(o => o.Trim()).ToList(),
            correctOptionIndex,
            string.IsNullOrWhiteSpace(hintCode) ? null : hintCode.Trim(),
            string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim());
    }

    /// <summary>True when <paramref name="selectedOptionIndex"/> is the correct choice.</summary>
    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;

    /// <summary>
    /// Typed practice uses the same canonical answer as the choice exercise.
    /// Ignore casing, surrounding whitespace and terminal punctuation, but never
    /// guess a different grammatical form or trust a client-supplied correctness flag.
    /// An unknown answer remains -1 and is graded wrong by GradeExercises.
    /// </summary>
    public int MatchTextAnswer(string answer)
    {
        var normalized = NormalizeAnswer(answer);
        if (normalized.Length == 0) return -1;
        for (var index = 0; index < Options.Count; index++)
        {
            if (NormalizeAnswer(Options[index]) == normalized) return index;
            if (Type != GrammarExerciseType.FillInBlank) continue;
            var gap = System.Text.RegularExpressions.Regex.Match(Prompt, @"_{2,}|…");
            if (!gap.Success) continue;
            var before = Prompt[..gap.Index].Trim();
            var after = Prompt[(gap.Index + gap.Length)..].Trim();
            var option = Options[index].Trim();
            if (option.StartsWith(before, StringComparison.OrdinalIgnoreCase) &&
                option.TrimEnd('.', '!', '?').EndsWith(after.TrimEnd('.', '!', '?'), StringComparison.OrdinalIgnoreCase))
            {
                var endLength = option.EndsWith(after, StringComparison.OrdinalIgnoreCase) ? after.Length : after.TrimEnd('.', '!', '?').Length;
                var cleaned = option.EndsWith(after, StringComparison.OrdinalIgnoreCase) ? option : option.TrimEnd('.', '!', '?');
                if (cleaned.Length >= before.Length + endLength &&
                    NormalizeAnswer(cleaned.Substring(before.Length, cleaned.Length - before.Length - endLength)) == normalized)
                    return index;
            }
        }
        return -1;
    }

    private static string NormalizeAnswer(string value) =>
        System.Text.RegularExpressions.Regex.Replace(value.Trim().TrimEnd('.', '!', '?').Trim(), @"\s+", " ")
            .Replace('’', '\'').ToLowerInvariant();
}
