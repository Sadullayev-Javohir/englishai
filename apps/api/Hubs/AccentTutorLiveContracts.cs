using Application.Speaking.AccentTutors;
using Application.Speaking.Dtos;

namespace Web.Hubs;

public sealed record AccentTutorLiveSessionReady(string TutorId, DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLiveStartTurnRequest(
    string TutorId,
    Guid TurnId,
    long Sequence,
    IReadOnlyList<AccentTutorMessage>? History,
    bool IsInterruption);

public sealed record AccentTutorLiveAudioChunkRequest(
    string TutorId,
    Guid TurnId,
    long Sequence,
    string PcmBase64);

public sealed record AccentTutorLiveTurnSignal(string TutorId, Guid TurnId, long Sequence);

public sealed record AccentTutorLiveLifecycleEvent(
    string TutorId,
    Guid TurnId,
    long Sequence,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLivePartialTranscript(
    string TutorId,
    Guid TurnId,
    long Sequence,
    string Text,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLiveTurnDiscarded(
    string TutorId,
    Guid TurnId,
    long Sequence,
    string Reason,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLiveTurnCompleted(
    string TutorId,
    Guid TurnId,
    long Sequence,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLiveFinalTranscript(
    string TutorId,
    Guid TurnId,
    long Sequence,
    string Transcript,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLiveTutorText(
    string TutorId,
    Guid TurnId,
    long Sequence,
    string TutorText,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLivePronunciation(
    string TutorId,
    Guid TurnId,
    long Sequence,
    PronunciationResultDto Pronunciation,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLiveTutorAudio(
    string TutorId,
    Guid TurnId,
    long Sequence,
    string TutorAudioBase64,
    bool IsNaturalVoice,
    IReadOnlyList<SpeechWordTimingDto> WordTimings,
    DateTimeOffset ServerTimestamp);

public sealed record AccentTutorLiveError(
    string TutorId,
    Guid TurnId,
    long Sequence,
    string Code,
    string Message,
    bool Recoverable,
    DateTimeOffset ServerTimestamp);
