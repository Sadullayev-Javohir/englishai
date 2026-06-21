using Application.Video.Dtos;

namespace Application.Video.Models;

/// <summary>Server-only answer key, grounded in a real transcript segment.</summary>
public sealed record GeneratedVideoQuestion(
    Guid Id, string Prompt, string PromptUz, IReadOnlyList<string> Options,
    int CorrectOptionIndex, double SourceStartSeconds, double SourceEndSeconds,
    string SourceText, string? SourceTranslation, string ExplanationUz);

public sealed record GeneratedVideoQuiz(
    Guid Id, Guid VideoLessonId, Guid LearnerId, string Title, string YouTubeVideoId,
    DateTimeOffset ExpiresAt, IReadOnlyList<GeneratedVideoQuestion> Questions,
    VideoQuizResultDto? Result = null);

public sealed class VideoQuizUnavailableException(string code) : Exception(code)
{
    public string Code { get; } = code;
}
