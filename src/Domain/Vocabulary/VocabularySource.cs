namespace Domain.Vocabulary;

/// <summary>
/// Where a vocabulary item was first encountered. Lets the SRS surface words from the
/// module the learner met them in, and lets future modules (e.g. Video in Faza 4) feed
/// saved words straight into the review schedule (PROJECT-SPEC Faza 4 integration note).
/// </summary>
public enum VocabularySource
{
    /// <summary>Added directly by the learner.</summary>
    Manual = 0,

    /// <summary>Captured from an AI speaking conversation.</summary>
    Speaking = 1,

    /// <summary>Saved while watching a video lesson.</summary>
    Video = 2,

    /// <summary>Saved from a reading passage.</summary>
    Reading = 3,

    /// <summary>Saved from a vocabulary topic passage (PROJECT-SPEC module 4).</summary>
    Vocabulary = 4
}
