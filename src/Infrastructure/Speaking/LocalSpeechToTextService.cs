using Application.Speaking.Ports;

namespace Infrastructure.Speaking;

/// <summary>
/// Offline stand-in used when Azure Speech is not configured. It must never invent
/// a transcript because that would present fabricated speech as the learner's words.
/// </summary>
public sealed class LocalSpeechToTextService : ISpeechToTextService
{
    public Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(SpeechTranscription.Rejected(SpeechTranscriptionRejection.ServiceFailure));
}
