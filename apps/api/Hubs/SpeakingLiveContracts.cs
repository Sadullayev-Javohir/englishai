using Application.Speaking.Dtos;
using Application.Vocabulary.Dtos;

namespace Web.Hubs;

public sealed record SpeakingLiveSessionReady(
    Guid SessionId,
    bool Enabled,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveTurnRequest(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string AudioBase64,
    string? ConfirmedTranscript = null,
    string? ConfirmationOutcome = null);

public sealed record SpeakingLivePartialTranscript(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string Text,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveFinalTranscript(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string Text,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveTutorText(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string Text,
    bool SessionLimitReached,
    bool NamePrompt,
    TopicSpeakingProgressDto? TopicProgress,
    TopicCompletionDto? Completion,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLivePronunciation(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    PronunciationResultDto Pronunciation,
    string? FeedbackUz,
    string? FocusWord,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveTutorAudio(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string AudioBase64,
    IReadOnlyList<VisemeFrameDto> Visemes,
    string? VisemeAnimation,
    bool IsNaturalVoice,
    IReadOnlyList<SpeechWordTimingDto> WordTimings,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveUnrecognized(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string? FeedbackUz,
    string RejectionCode,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveTranscriptAlternative(string Text, double Confidence);

public sealed record SpeakingLiveTranscriptConfirmation(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string SuggestedText,
    IReadOnlyList<SpeakingLiveTranscriptAlternative> Alternatives,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveTurnCompleted(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    DateTimeOffset ServerTimestamp);

public sealed record SpeakingLiveError(
    Guid SessionId,
    Guid TurnId,
    long Sequence,
    string Code,
    string Message,
    bool Recoverable,
    DateTimeOffset ServerTimestamp);
