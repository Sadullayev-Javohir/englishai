namespace Domain.Speaking;

/// <summary>
/// The four dimensions a roleplay performance is scored on. The end-of-scene evaluation reports a
/// 0-100 score for each; the Uzbek summary/tip shown to the learner is chosen from templates by the
/// weakest and strongest dimension (docs/development-guide.md rule 11 - no free-generated Uzbek text).
/// </summary>
public enum RoleplayDimension
{
    /// <summary>Did the learner accomplish the scenario's objective (e.g. actually book a table)?</summary>
    TaskCompletion = 1,

    /// <summary>How smoothly and confidently the learner communicated.</summary>
    Fluency = 2,

    /// <summary>Grammatical accuracy and range/appropriateness of vocabulary.</summary>
    Grammar = 3,

    /// <summary>Register and politeness - was the language appropriate for the situation?</summary>
    Appropriateness = 4
}
