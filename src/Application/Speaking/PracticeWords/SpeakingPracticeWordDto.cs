using Domain.Speaking;

namespace Application.Speaking.PracticeWords;

public sealed record SpeakingPracticeWordDto(
    Guid Id,
    string Word,
    double LastAccuracyScore,
    PronunciationErrorType LastErrorType,
    int ErrorCount,
    double? BestPracticeScore,
    DateTimeOffset FirstFailedAt,
    DateTimeOffset LastFailedAt)
{
    public static SpeakingPracticeWordDto FromDomain(SpeakingPracticeWord word) => new(
        word.Id,
        word.Word,
        Math.Round(word.LastAccuracyScore, 1),
        word.LastErrorType,
        word.ErrorCount,
        word.BestPracticeScore is null ? null : Math.Round(word.BestPracticeScore.Value, 1),
        word.FirstFailedAt,
        word.LastFailedAt);
}
