using System.Text.RegularExpressions;

namespace Application.Speaking.AccentTutors;

public enum UtteranceEndpointDecision
{
    Wait,
    Commit,
    Discard,
}

public enum UtteranceSemanticState
{
    Complete,
    Incomplete,
    Uncertain,
}

public sealed record UtteranceEndpointSnapshot(
    TimeSpan AcousticSilence,
    string PartialTranscript,
    TimeSpan TranscriptStableFor,
    TimeSpan TurnDuration);

public interface IUtteranceSemanticClassifier
{
    Task<UtteranceSemanticState> ClassifyAsync(
        string transcript,
        CancellationToken cancellationToken = default);
}

public interface IUtteranceEndpointDetector
{
    Task<UtteranceEndpointDecision> DetectAsync(
        UtteranceEndpointSnapshot snapshot,
        CancellationToken cancellationToken = default);
}

public sealed partial class UtteranceEndpointDetector(
    IUtteranceSemanticClassifier semanticClassifier) : IUtteranceEndpointDetector
{
    // Tuned for language learners, who pause mid-sentence to search for words. Committing too
    // eagerly cut utterances in half; these give more silence before a turn is finalized.
    public static readonly TimeSpan MinimumPause = TimeSpan.FromMilliseconds(800);
    public static readonly TimeSpan CompletePause = TimeSpan.FromMilliseconds(1_200);
    public static readonly TimeSpan IncompletePause = TimeSpan.FromMilliseconds(2_200);
    public static readonly TimeSpan ForcedPause = TimeSpan.FromMilliseconds(3_000);
    public static readonly TimeSpan MaximumTurn = TimeSpan.FromSeconds(30);
    public static readonly TimeSpan StableTranscript = TimeSpan.FromMilliseconds(300);

    public async Task<UtteranceEndpointDecision> DetectAsync(
        UtteranceEndpointSnapshot snapshot,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(snapshot.PartialTranscript) &&
            (snapshot.TurnDuration >= MaximumTurn || snapshot.AcousticSilence >= ForcedPause))
            return UtteranceEndpointDecision.Discard;
        if (snapshot.TurnDuration >= MaximumTurn || snapshot.AcousticSilence >= ForcedPause)
            return UtteranceEndpointDecision.Commit;
        if (snapshot.AcousticSilence < MinimumPause || snapshot.TranscriptStableFor < StableTranscript)
            return UtteranceEndpointDecision.Wait;

        var deterministic = ClassifyDeterministically(snapshot.PartialTranscript);
        if (deterministic == UtteranceSemanticState.Incomplete)
            return snapshot.AcousticSilence >= IncompletePause
                ? UtteranceEndpointDecision.Commit
                : UtteranceEndpointDecision.Wait;
        if (deterministic == UtteranceSemanticState.Complete)
            return snapshot.AcousticSilence >= CompletePause
                ? UtteranceEndpointDecision.Commit
                : UtteranceEndpointDecision.Wait;
        if (snapshot.AcousticSilence < CompletePause)
            return UtteranceEndpointDecision.Wait;

        var semantic = await semanticClassifier.ClassifyAsync(snapshot.PartialTranscript, cancellationToken);
        return semantic switch
        {
            UtteranceSemanticState.Complete => UtteranceEndpointDecision.Commit,
            UtteranceSemanticState.Incomplete when snapshot.AcousticSilence < IncompletePause => UtteranceEndpointDecision.Wait,
            _ when snapshot.AcousticSilence >= IncompletePause => UtteranceEndpointDecision.Commit,
            _ => UtteranceEndpointDecision.Wait,
        };
    }

    public static UtteranceSemanticState ClassifyDeterministically(string transcript)
    {
        var text = Whitespace().Replace(transcript ?? string.Empty, " ").Trim();
        if (text.Length == 0) return UtteranceSemanticState.Uncertain;

        var normalized = text.TrimEnd('.', '!', '?', ',', ';', ':', '…').ToLowerInvariant();
        if (ExplicitlyIncomplete().IsMatch(normalized) || IncompleteTail().IsMatch(normalized))
            return UtteranceSemanticState.Incomplete;
        if (text.EndsWith('.') || text.EndsWith('!') || text.EndsWith('?'))
            return UtteranceSemanticState.Complete;

        return UtteranceSemanticState.Uncertain;
    }

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();

    [GeneratedRegex(@"(?:^|\s)(?:i think that|because i|the reason is|what i mean is|if i|when i|although i|so that)$", RegexOptions.IgnoreCase)]
    private static partial Regex ExplicitlyIncomplete();

    [GeneratedRegex(@"(?:^|\s)(?:and|or|but|because|so|if|when|while|although|though|that|to|of|for|from|with|about|at|in|on|by|is|am|are|was|were|be|been|being|do|does|did|have|has|had|can|could|will|would|shall|should|may|might|must|who|what|where|when|why|how|which)$", RegexOptions.IgnoreCase)]
    private static partial Regex IncompleteTail();
}
