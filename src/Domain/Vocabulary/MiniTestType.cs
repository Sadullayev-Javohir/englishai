namespace Domain.Vocabulary;

/// <summary>
/// The kind of active-retrieval mini-test presented at a review (PROJECT-SPEC B.1).
/// The type varies as the word climbs the ladder so the learner uses the word in a
/// new context each time rather than re-seeing the same flashcard.
/// </summary>
public enum MiniTestType
{
    /// <summary>Fill-in-the-blank: pick the word that fits a short generated sentence.</summary>
    ClozeChoice = 0,

    /// <summary>Written production: write one sentence using the word.</summary>
    WrittenUsage = 1,

    /// <summary>Spoken production: answer aloud using the word (joins pronunciation).</summary>
    SpokenUsage = 2,

    /// <summary>Listening recognition: hear the word in a clip and pick its meaning.</summary>
    ListeningRecognition = 3
}
