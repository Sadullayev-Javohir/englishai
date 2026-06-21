using Application.Video.Models;

namespace Application.Video.Dtos;

public sealed record GeneratedVideoQuestionDto(
    Guid Id, string Prompt, string PromptUz, IReadOnlyList<string> Options,
    double SourceStartSeconds, double SourceEndSeconds, string SourceText);

/// <summary>No answers or explanations are exposed before a complete submission.</summary>
public sealed record GeneratedVideoQuizDto(
    Guid QuizId, Guid VideoLessonId, string Title, string YouTubeVideoId, DateTimeOffset ExpiresAt,
    IReadOnlyList<GeneratedVideoQuestionDto> Questions, VideoQuizResultDto? Result)
{
    public static GeneratedVideoQuizDto FromSession(GeneratedVideoQuiz quiz) =>
        new(quiz.Id, quiz.VideoLessonId, quiz.Title, quiz.YouTubeVideoId, quiz.ExpiresAt,
            quiz.Questions.Select(q => new GeneratedVideoQuestionDto(q.Id, q.Prompt, q.PromptUz, q.Options,
                q.SourceStartSeconds, q.SourceEndSeconds, q.SourceText)).ToArray(), quiz.Result);
}
