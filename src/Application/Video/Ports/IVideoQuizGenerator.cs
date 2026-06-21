using Application.Video.Models;
using Domain.Assessment;
using Domain.Video;

namespace Application.Video.Ports;

public interface IVideoQuizGenerator
{
    Task<IReadOnlyList<GeneratedVideoQuestion>> GenerateAsync(
        string title, CefrLevel level, IReadOnlyList<TranscriptSegment> transcript,
        CancellationToken cancellationToken);
}

public interface IVideoQuizStore
{
    Task<GeneratedVideoQuiz?> GetAsync(Guid quizId, CancellationToken cancellationToken);
    Task SaveAsync(GeneratedVideoQuiz quiz, CancellationToken cancellationToken);
}
