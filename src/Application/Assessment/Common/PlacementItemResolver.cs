using Application.Assessment.Dtos;
using Application.Assessment.Ports;
using Domain.Assessment;

namespace Application.Assessment.Common;

/// <summary>
/// Builds the next <see cref="PlacementItemDto"/> for a session after it has advanced.
/// Centralizes the branch between the adaptive multiple-choice bank and the curated
/// productive (Writing/Speaking) task providers so every handler resolves the next item
/// the same way.
/// </summary>
public static class PlacementItemResolver
{
    public static async Task<PlacementItemDto?> NextItemAsync(
        PlacementTestSession session,
        IPlacementQuestionRepository questions,
        IPlacementProductiveTaskProvider tasks,
        CancellationToken cancellationToken)
    {
        if (session.IsCompleted)
            return null;

        if (session.CurrentItemId is Guid currentItemId)
        {
            if (session.CurrentStage == TestStage.Writing)
            {
                var writing = tasks.GetWritingTask(session.CurrentDifficulty);
                return writing.Id == currentItemId ? PlacementItemDto.FromWritingTask(writing, session) : null;
            }

            if (session.CurrentStage == TestStage.Speaking)
            {
                var speaking = tasks.GetSpeakingTask(session.CurrentDifficulty);
                return speaking.Id == currentItemId ? PlacementItemDto.FromSpeakingTask(speaking, session) : null;
            }

            var currentQuestion = await questions.GetByIdAsync(currentItemId, cancellationToken);
            return currentQuestion is null ? null : PlacementItemDto.FromQuestion(currentQuestion, session);
        }

        if (session.CurrentStage == TestStage.Writing)
        {
            var writing = tasks.GetWritingTask(session.CurrentDifficulty);
            session.ServeItem(writing.Id);
            return PlacementItemDto.FromWritingTask(writing, session);
        }

        if (session.CurrentStage == TestStage.Speaking)
        {
            var speaking = tasks.GetSpeakingTask(session.CurrentDifficulty);
            session.ServeItem(speaking.Id);
            return PlacementItemDto.FromSpeakingTask(speaking, session);
        }

        var excludeIds = session.Answers.Select(a => a.QuestionId).ToList();
        var next = await questions.GetNextAsync(
            session.CurrentStage,
            session.CurrentDifficulty,
            excludeIds,
            session.Id,
            cancellationToken);

        if (next is null)
            return null;

        session.ServeItem(next.Id);
        return PlacementItemDto.FromQuestion(next, session);
    }
}
