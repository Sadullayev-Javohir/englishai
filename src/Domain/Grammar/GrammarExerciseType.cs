namespace Domain.Grammar;

/// <summary>
/// The auto-gradable exercise types in a grammar lesson (PROJECT-SPEC G.2 steps 3-4).
/// <see cref="Recognition"/> is step 3 ("tanib olish" - choose the correct/incorrect form);
/// <see cref="FillInBlank"/> and <see cref="Rephrase"/> are step 4 ("faol ishlatish" -
/// fill the gap or rewrite the sentence). All three are presented as multiple choice so
/// they can be graded server-side without an LLM.
/// </summary>
public enum GrammarExerciseType
{
    Recognition = 1,
    FillInBlank = 2,
    Rephrase = 3
}
