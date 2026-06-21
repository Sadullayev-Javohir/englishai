using Application.Speaking.Dtos;
using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Speaking.SubmitUtterance;

/// <summary>
/// Submits the learner's recorded audio for the current conversation turn. The
/// handler runs the full pipeline: STT -> pronunciation assessment -> tutor reply
/// -> TTS+visemes, and returns Uzbek feedback (from templates).
/// </summary>
public sealed record SubmitUtteranceCommand(
    Guid SessionId,
    byte[] AudioContent,
    string? ConfirmedTranscript = null,
    string? ConfirmationOutcome = null)
    : IRequest<SubmitUtteranceResult>;

/// <param name="Recognized">
/// False when speech-to-text returned nothing (silence or unintelligible audio). The
/// other fields are then empty and the client should prompt the learner to retry -
/// an unrecognized clip is not an error, just a missed turn.
/// </param>
/// <param name="NamePrompt">
/// True when the learner appeared to introduce or spell their name but the tutor still has no
/// stored name for them. Spoken names (especially spelled-out and Uzbek names) are exactly what the
/// English STT model garbles, so the client surfaces a small input for the learner to type their
/// name; once saved the tutor addresses them by it from the next turn.
/// </param>
public sealed record SubmitUtteranceResult(
    bool Recognized,
    string RecognizedText,
    PronunciationResultDto? Pronunciation,
    string TutorText,
    string TutorAudioBase64,
    IReadOnlyList<VisemeFrameDto> Visemes,
    string? VisemeAnimation,
    string? FeedbackUz,
    string? FocusWord,
    TopicSpeakingProgressDto? TopicProgress = null,
    TopicCompletionDto? Completion = null,
    bool SessionLimitReached = false,
    bool NamePrompt = false,
    bool IsNaturalVoice = true,
    IReadOnlyList<SpeechWordTimingDto>? WordTimings = null,
    string? RejectionCode = null,
    bool RequiresTranscriptConfirmation = false,
    IReadOnlyList<TranscriptAlternativeDto>? TranscriptAlternatives = null);

public sealed record TranscriptAlternativeDto(string Text, double Confidence);
