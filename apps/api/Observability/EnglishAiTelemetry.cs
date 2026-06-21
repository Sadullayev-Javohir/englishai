using System.Diagnostics.Metrics;

namespace Web.Observability;

public static class EnglishAiTelemetry
{
    public const string ServiceName = "EnglishAI.Web";
    public const string MeterName = "EnglishAI.Observability";
    public static readonly Meter Meter = new(MeterName);

    public static readonly UpDownCounter<long> SignalRConnections =
        Meter.CreateUpDownCounter<long>("englishai.signalr.connections", unit: "{connection}");

    public static readonly Counter<long> SignalRConnectionEvents =
        Meter.CreateCounter<long>("englishai.signalr.connection.events", unit: "{event}");

    public static readonly Counter<long> SignalRGroupEvents =
        Meter.CreateCounter<long>("englishai.signalr.group.events", unit: "{event}");

    public static readonly Counter<long> SignalRSendEvents =
        Meter.CreateCounter<long>("englishai.signalr.send.events", unit: "{event}");

    public static readonly Histogram<double> SpeakingLiveTurnDuration =
        Meter.CreateHistogram<double>("englishai.speaking.live.turn.duration", unit: "ms");

    public static readonly Counter<long> SpeakingLiveInterruptions =
        Meter.CreateCounter<long>("englishai.speaking.live.interruptions", unit: "{interruption}");

    public static readonly Counter<long> SpeakingLiveErrors =
        Meter.CreateCounter<long>("englishai.speaking.live.errors", unit: "{error}");
}
