using Application.Backfill.Models;
using Application.Backfill.Ports;
using Application.Listening.Ports;
using Application.Vocabulary;
using Domain.Common;
using Domain.Listening;
using Domain.Vocabulary;
using Infrastructure.Listening;
using Infrastructure.Llm;

namespace Infrastructure.Backfill;

/// <summary>
/// Backfills a topic's listening exercise (transcript + comprehension questions). Reuses
/// <see cref="LlmListeningContentGenerator"/>'s prompt and parser and mirrors the persist logic of
/// <c>GetListeningExerciseQueryHandler</c>. The transcript's audio is synthesized lazily on first
/// play and cached separately (docs/development-guide.md rule 10).
/// </summary>
public sealed class ListeningContentBackfiller : IContentBackfiller
{
    private readonly IListeningRepository _exercises;
    private readonly string _model;

    public ListeningContentBackfiller(IListeningRepository exercises, HermesGatewayOptions gateway)
    {
        _exercises = exercises;
        _model = ContentLlm.Model(gateway);
    }

    public string Module => "listening";

    public async Task<BatchContentRequest?> BuildRequestAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        var existing = await _exercises.GetByTopicIdAsync(topic.Id, cancellationToken);
        if (existing is { IsFilled: true })
            return null;

        return new BatchContentRequest(
            $"{Module}_{topic.Id}",
            _model,
            LlmListeningContentGenerator.SystemPrompt,
            LlmListeningContentGenerator.BuildUserPrompt(
                topic.Title, topic.Level, TopicTargetWords.Of(topic)),
            LlmListeningContentGenerator.MaxOutputTokens);
    }

    public async Task<bool> ApplyResultAsync(VocabularyTopic topic, string responseText, CancellationToken cancellationToken)
    {
        var content = LlmListeningContentGenerator.Parse(responseText);
        if (!content.HasContent)
            return false;

        var questions = content.Questions
            .Where(q => !string.IsNullOrWhiteSpace(q.Prompt) && q.Options.Count >= ListeningQuestion.MinOptions
                        && q.CorrectOptionIndex >= 0 && q.CorrectOptionIndex < q.Options.Count)
            .Select(q => ListeningQuestion.Create(q.Prompt, q.Options, q.CorrectOptionIndex, null, q.Explanation))
            .ToList();
        if (questions.Count == 0)
            return false;

        try
        {
            var existing = await _exercises.GetByTopicIdAsync(topic.Id, cancellationToken);
            if (existing is { IsFilled: true })
                return false;

            var exercise = existing ?? ListeningExercise.ForTopic(
                topic.Id, topic.Title, topic.Category, topic.Level, DateTimeOffset.UtcNow);
            exercise.FillContent(content.Transcript, questions);
            await _exercises.SaveAsync(exercise, cancellationToken);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}
