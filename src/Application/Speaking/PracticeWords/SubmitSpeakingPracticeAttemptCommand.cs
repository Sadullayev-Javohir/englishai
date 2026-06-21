using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Speaking.PracticeWords;

public sealed record SubmitSpeakingPracticeAttemptCommand(Guid PracticeWordId, byte[] AudioContent)
    : IRequest<SpeakingPracticeAttemptResult>;

public sealed record SpeakingPracticeAttemptResult(
    WordPronunciationCheckDto Assessment,
    bool Mastered,
    double MasteryScore,
    int SuccessfulAttempts,
    int RequiredSuccesses);
