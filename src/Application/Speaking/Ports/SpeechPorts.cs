using Application.Speaking.Models;
using Domain.Speaking;

namespace Application.Speaking.Ports;

/// <summary>Speech-to-text (Azure STT in production; swappable via this port).</summary>
public interface ISpeechToTextService
{
    Task<SpeechTranscription> TranscribeAsync(byte[] audioContent, CancellationToken cancellationToken = default);
}

public interface IContextualSpeechToTextService : ISpeechToTextService
{
    Task<SpeechTranscription> TranscribeAsync(
        byte[] audioContent,
        SpeechRecognitionContext context,
        CancellationToken cancellationToken = default);
}

public interface IStreamingSpeechToTextSession : IAsyncDisposable
{
    Task StartAsync(CancellationToken cancellationToken = default);
    Task WriteAsync(ReadOnlyMemory<byte> pcm, CancellationToken cancellationToken = default);
    Task<SpeechTranscription> CompleteAsync(CancellationToken cancellationToken = default);
}

public interface IStreamingSpeechToTextService
{
    IStreamingSpeechToTextSession CreateSession(
        SpeechRecognitionContext context,
        Func<string, Task>? onPartialTranscript = null);
}

public sealed record SpeechRecognitionContext(
    string? LastTutorPrompt,
    string? Topic,
    IReadOnlyList<string> FocusWords,
    IReadOnlyList<string> ContextPhrases,
    bool IncludeNameHints,
    string? RecognitionLanguage = null);

public sealed record SpeechTranscriptionCandidate(string Text, double Confidence);

public enum SpeechTranscriptionRejection
{
    None,
    InvalidAudio,
    NoSpeech,
    LowConfidence,
    ServiceFailure,
}

public sealed record SpeechTranscription(
    string Text,
    double? Confidence,
    SpeechTranscriptionRejection Rejection,
    IReadOnlyList<SpeechTranscriptionCandidate>? Alternatives = null,
    bool RequiresConfirmation = false,
    double? ScoreGap = null)
{
    public IReadOnlyList<SpeechTranscriptionCandidate> Candidates { get; } =
        Alternatives ?? Array.Empty<SpeechTranscriptionCandidate>();

    public bool IsAccepted =>
        Rejection == SpeechTranscriptionRejection.None && !string.IsNullOrWhiteSpace(Text);

    public static SpeechTranscription Accepted(
        string text,
        double? confidence = null,
        IReadOnlyList<SpeechTranscriptionCandidate>? alternatives = null,
        bool requiresConfirmation = false,
        double? scoreGap = null) =>
        new(
            text.Trim(),
            confidence,
            SpeechTranscriptionRejection.None,
            alternatives,
            requiresConfirmation,
            scoreGap);

    public static SpeechTranscription Rejected(SpeechTranscriptionRejection rejection) =>
        new(string.Empty, null, rejection);
}

/// <summary>
/// Pronunciation assessment (Azure Pronunciation Assessment in production). Scores
/// the audio against the recognized/reference text.
/// </summary>
public interface IPronunciationAssessor
{
    Task<PronunciationResult> AssessAsync(
        byte[] audioContent,
        string referenceText,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Text-to-speech that also returns visemes (Azure Neural TTS). Implementations
/// should cache common/repeated phrases (docs/development-guide.md rule 10).
/// </summary>
public interface ITextToSpeechService
{
    Task<SynthesizedSpeech> SynthesizeAsync(string text, CancellationToken cancellationToken = default);
}

/// <summary>
/// Text-to-speech that can synthesize in a named voice rather than only the configured default -
/// what the accent tutors need to sound American, British, Australian or Irish.
///
/// This exists as an interface so callers can ask for the capability instead of type-checking for a
/// concrete adapter. A decorator (caching, tiering) can then wrap the service without silently
/// dropping the per-tutor voice, which is exactly what an <c>is AzureTextToSpeechService</c> check
/// would do.
/// </summary>
public interface IVoicedTextToSpeechService : ITextToSpeechService
{
    Task<SynthesizedSpeech> SynthesizeWithVoiceAsync(
        string text,
        string voiceName,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Stores synthesized speech so a repeated phrase is paid for once. Azure charges per character, and
/// the conversation openings, roleplay openers and stock tutor lines repeat across every learner -
/// caching them is required by docs/development-guide.md rule 10.
///
/// Implementations MUST round-trip the whole <see cref="SynthesizedSpeech"/>: audio, the viseme
/// sequence (including its whole-utterance SVG animation), the word timings and the natural-voice
/// flag. Returning audio alone would silently break the mouth animation and transcript highlighting.
/// </summary>
public interface ISynthesizedSpeechCache
{
    Task<SynthesizedSpeech?> GetAsync(string voiceName, string text, CancellationToken cancellationToken = default);

    Task SetAsync(
        string voiceName,
        string text,
        SynthesizedSpeech speech,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// LLM tutor that produces the next reply for a conversation. Implementations must
/// use a budget model and cap output tokens (docs/development-guide.md rule 10).
/// </summary>
public interface IConversationTutor
{
    Task<string> NextReplyAsync(ConversationSession session, CancellationToken cancellationToken = default);
}
