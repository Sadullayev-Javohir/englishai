namespace Domain.Learning;

/// <summary>
/// The six language skills tracked per learner for the CEFR level-transition
/// formula (PROJECT-SPEC G.4). Each carries an independent 0-100 skill score.
/// These are broader than the placement <see cref="Domain.Assessment.TestStage"/>:
/// the placement independently seeds each of Vocabulary, Grammar, and the other skills, and
/// Writing has no placement stage (it is seeded from the overall level).
/// </summary>
public enum SkillType
{
    Speaking = 1,
    Listening = 2,
    Reading = 3,
    Writing = 4,
    Grammar = 5,
    Vocabulary = 6
}
