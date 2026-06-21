namespace Domain.Competition;

/// <summary>Lifecycle of a competition. Host-driven transitions only (see Competition).</summary>
public enum CompetitionStatus
{
    Draft = 0,
    Lobby = 1,
    Active = 2,
    Finished = 3,
}

/// <summary>Where a slide's question was sourced from when the competition was built.</summary>
public enum SlideSourceType
{
    Vocabulary = 0,
    Grammar = 1,
}

/// <summary>A participant's progress through the live competition.</summary>
public enum ParticipantStatus
{
    Joined = 0,
    Playing = 1,
    Finished = 2,
}
