using System.Text.Json;
using Application.Video.Models;
using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Uses the existing bounded cache (Redis in production, in-memory locally), with a separate key
/// namespace. Session ownership and its shorter two-hour expiry are enforced in Application.
/// </summary>
public sealed class VideoQuizStore(IVideoExplainCache cache) : IVideoQuizStore
{
    public async Task<GeneratedVideoQuiz?> GetAsync(Guid quizId, CancellationToken cancellationToken)
    {
        var value = await cache.GetAsync($"quiz-session:v1:{quizId}", cancellationToken);
        return value is null ? null : JsonSerializer.Deserialize<GeneratedVideoQuiz>(value);
    }

    public Task SaveAsync(GeneratedVideoQuiz quiz, CancellationToken cancellationToken) =>
        cache.SetAsync($"quiz-session:v1:{quiz.Id}", JsonSerializer.Serialize(quiz), cancellationToken);
}
