namespace Domain.Gamification;

/// <summary>The two activities that cost one energy to start.</summary>
public enum EnergyAction
{
    /// <summary>Opening a video lesson. Reference id is the YouTube video id.</summary>
    Video = 1,

    /// <summary>Starting a speaking conversation. Reference id is the session id.</summary>
    Speaking = 2,
}
