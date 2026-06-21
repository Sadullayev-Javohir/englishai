namespace Domain.Video;

/// <summary>
/// A learner's "was it easy/hard?" judgement of a video (PROJECT-SPEC B.3, Bosqich 2).
/// These ratings are the training signal for the recommendation model: the catalog can
/// nudge a learner toward easier or harder content based on accumulated feedback.
/// </summary>
public enum VideoDifficultyRating
{
    /// <summary>Too easy - the learner wants more challenging content.</summary>
    TooEasy = 0,

    /// <summary>Well matched to the learner's level.</summary>
    JustRight = 1,

    /// <summary>Too hard - the learner wants easier content.</summary>
    TooHard = 2
}
