using Application.Backfill.Models;
using Application.Backfill.Ports;
using Application.Vocabulary;
using Application.Writing;
using Application.Writing.Ports;
using Domain.Common;
using Domain.Vocabulary;
using Domain.Writing;
using Infrastructure.Llm;
using Infrastructure.Writing;

namespace Infrastructure.Backfill;

/// <summary>
/// Backfills a topic's writing task (prompt + guidance). Reuses
/// <see cref="HermesWritingPromptGenerator"/>'s prompt and parser and mirrors the persist logic of
/// <c>GetWritingTaskQueryHandler</c>.
/// </summary>
public sealed class WritingContentBackfiller : IContentBackfiller
{
    private readonly IWritingTaskRepository _tasks;
    private readonly HermesGatewayOptions _hermesGateway;

    public WritingContentBackfiller(IWritingTaskRepository tasks, HermesGatewayOptions hermesGateway)
    {
        _tasks = tasks;
        _hermesGateway = hermesGateway;
    }

    public string Module => "writing";

    public async Task<BatchContentRequest?> BuildRequestAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        var existing = await _tasks.GetByTopicIdAsync(topic.Id, cancellationToken);
        if (existing is { IsFilled: true })
            return null;

        // Writing backfill runs on the same backend as the bulk modules - the Hermes gateway (docs/development-guide.md
        // rule 10, zero-cost catalogue fill against a self-hosted model). The model id here is only an
        // observability tag; actual dispatch happens inside the gateway completion.
        return new BatchContentRequest(
            $"{Module}_{topic.Id}",
            _hermesGateway.Model,
            HermesWritingPromptGenerator.SystemPrompt,
            HermesWritingPromptGenerator.BuildUserPrompt(
                topic.Title, topic.Level, TopicTargetWords.Of(topic)),
            HermesWritingPromptGenerator.MaxOutputTokens);
    }

    public async Task<bool> ApplyResultAsync(VocabularyTopic topic, string responseText, CancellationToken cancellationToken)
    {
        var content = HermesWritingPromptGenerator.Parse(responseText);
        if (!content.HasContent)
            return false;

        try
        {
            var existing = await _tasks.GetByTopicIdAsync(topic.Id, cancellationToken);
            if (existing is { IsFilled: true })
                return false;

            var (min, max) = WritingWordRange.For(topic.Level);
            var task = existing ?? WritingTask.ForTopic(topic.Id, topic.Level, min, max, DateTimeOffset.UtcNow);
            task.FillContent(content.Prompt, content.Guidance);
            await _tasks.SaveAsync(task, cancellationToken);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}
