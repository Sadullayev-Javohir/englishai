using Application.Speaking.AccentTutors;
using Infrastructure.Llm;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Speaking;

public sealed class HermesUtteranceSemanticClassifier(
    ILlmCompletion? completion,
    ILogger<HermesUtteranceSemanticClassifier> logger) : IUtteranceSemanticClassifier
{
    public async Task<UtteranceSemanticState> ClassifyAsync(
        string transcript,
        CancellationToken cancellationToken = default)
    {
        if (completion is null)
            return UtteranceEndpointDetector.ClassifyDeterministically(transcript);

        try
        {
            var answer = await completion.CompleteAsync(
                "Classify whether a live English learner has finished their thought. Reply with exactly complete or incomplete. " +
                "A trailing conjunction, auxiliary, preposition, question stem, or dependent clause is incomplete.",
                transcript,
                4,
                cancellationToken);
            return answer?.Trim().ToLowerInvariant() switch
            {
                "complete" => UtteranceSemanticState.Complete,
                "incomplete" => UtteranceSemanticState.Incomplete,
                _ => UtteranceEndpointDetector.ClassifyDeterministically(transcript),
            };
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            logger.LogDebug(exception, "Hermes utterance endpoint classification failed; using deterministic fallback.");
            return UtteranceEndpointDetector.ClassifyDeterministically(transcript);
        }
    }
}
