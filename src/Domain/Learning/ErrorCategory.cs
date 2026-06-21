namespace Domain.Learning;

/// <summary>
/// Categories that the error heatmap aggregates, ordered to match the Uzbek-learner
/// difficulty priorities from PROJECT-SPEC G.2 (articles first, then tense, ...).
/// The heatmap counts observations per category so the recommendation engine can
/// surface the learner's most frequent weakness. Uzbek-facing labels live in the
/// content layer (docs/development-guide.md rule 11), never here.
/// </summary>
public enum ErrorCategory
{
    Articles = 1,
    VerbTense = 2,
    Prepositions = 3,
    GerundInfinitive = 4,
    Modals = 5,
    SubjectVerbAgreement = 6,
    WordOrder = 7,
    Pronunciation = 8,
    Vocabulary = 9,
    Spelling = 10,
    Other = 99
}
