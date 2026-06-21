using System.Diagnostics.Metrics;

namespace Application.Speaking.Common;

public static class SpeakingRecognitionTelemetry
{
    private static readonly Meter Meter = new("EnglishAI.Observability");
    private static readonly Counter<long> Outcomes =
        Meter.CreateCounter<long>("englishai.speaking.transcript.outcomes", unit: "{outcome}");

    public static void Record(string outcome, double? confidence = null, double? scoreGap = null) =>
        Outcomes.Add(
            1,
            new KeyValuePair<string, object?>("outcome", outcome),
            new KeyValuePair<string, object?>("confidence_bucket", Bucket(confidence)),
            new KeyValuePair<string, object?>("score_gap_bucket", Bucket(scoreGap)));

    private static string Bucket(double? value) => value switch
    {
        null => "unknown",
        < 0.40 => "lt_040",
        < 0.60 => "040_059",
        < 0.75 => "060_074",
        < 0.90 => "075_089",
        _ => "gte_090",
    };
}
