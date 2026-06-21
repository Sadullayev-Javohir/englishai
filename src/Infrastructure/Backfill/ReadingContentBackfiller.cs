using Application.Backfill.Models;
using Application.Backfill.Ports;
using Application.Reading.Ports;
using Application.Vocabulary;
using Domain.Common;
using Domain.Reading;
using Domain.Vocabulary;
using Infrastructure.Llm;
using Infrastructure.Reading;

namespace Infrastructure.Backfill;

/// <summary>
/// Backfills a topic's reading lesson (passage + glossary + questions). Reuses
/// <see cref="LlmReadingContentGenerator"/>'s prompt and parser and mirrors the persist logic of
/// <c>GetReadingPassageQueryHandler</c>. Runs on the free Gemini model (docs/development-guide.md rule 10 - Claude is
/// reserved for speaking/writing); the batch runner routes each request by its model id.
/// </summary>
public sealed class ReadingContentBackfiller : IContentBackfiller
{
    private readonly IReadingRepository _passages;
    private readonly string _model;

    public ReadingContentBackfiller(IReadingRepository passages, HermesGatewayOptions gateway)
    {
        _passages = passages;
        _model = ContentLlm.Model(gateway);
    }

    public string Module => "reading";

    public async Task<BatchContentRequest?> BuildRequestAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        var existing = await _passages.GetByTopicIdAsync(topic.Id, cancellationToken);
        if (existing is { IsFilled: true })
            return null;

        return new BatchContentRequest(
            $"{Module}_{topic.Id}",
            _model,
            LlmReadingContentGenerator.SystemPrompt,
            LlmReadingContentGenerator.BuildUserPrompt(
                topic.Title, topic.Level, TopicTargetWords.Of(topic)),
            LlmReadingContentGenerator.MaxOutputTokens);
    }

    public async Task<bool> ApplyResultAsync(VocabularyTopic topic, string responseText, CancellationToken cancellationToken)
    {
        var content = LlmReadingContentGenerator.Parse(responseText);
        if (!content.HasContent)
            return false;

        var questions = content.Questions
            .Where(q => !string.IsNullOrWhiteSpace(q.Prompt) && q.Options.Count >= ReadingQuestion.MinOptions)
            .Select(q => ReadingQuestion.Create(q.Prompt, q.Options, q.CorrectOptionIndex, null, q.Explanation))
            .ToList();
        if (questions.Count == 0)
            return false;

        var glossary = content.Glossary
            .Where(g => !string.IsNullOrWhiteSpace(g.Word) && !string.IsNullOrWhiteSpace(g.Translation))
            .Select(g => GlossaryEntry.Create(g.Word, g.Translation, g.ExampleSentence))
            .ToList();

        try
        {
            var existing = await _passages.GetByTopicIdAsync(topic.Id, cancellationToken);
            if (existing is { IsFilled: true })
                return false;

            var passage = existing ?? ReadingPassage.ForTopic(
                topic.Id, topic.Title, topic.Category, topic.Level, DateTimeOffset.UtcNow);
            passage.FillContent(content.Body, glossary, questions);
            await _passages.SaveAsync(passage, cancellationToken);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}
