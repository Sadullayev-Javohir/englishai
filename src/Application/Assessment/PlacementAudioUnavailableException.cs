namespace Application.Assessment;

/// <summary>Listening cannot be assessed using a silent clip or a development test tone.</summary>
public sealed class PlacementAudioUnavailableException : Exception
{
    public const string ErrorCode = "placement_audio_unavailable";
    public PlacementAudioUnavailableException()
        : base("The listening voice service is unavailable. Please retry without leaving your test.") { }
}
