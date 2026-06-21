namespace Web.Hubs;

public sealed class SpeakingLiveOptions
{
    public const string SectionName = "SpeakingLive";

    public bool Enabled { get; set; }
    public int MaxAudioBytes { get; set; } = 2_000_000;
    public int ProcessedTurnCacheSize { get; set; } = 32;
}
