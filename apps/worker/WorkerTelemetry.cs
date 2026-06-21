using System.Diagnostics.Metrics;

namespace Worker;

public static class WorkerTelemetry
{
    public const string MeterName = "EnglishAI.Worker";
    public static readonly Meter Meter = new(MeterName);
}
