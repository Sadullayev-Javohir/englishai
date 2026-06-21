using Application.Backfill.Models;
using Application.Backfill.Ports;
using Application.Vocabulary.Ports;
using Domain.Common;
using Domain.Vocabulary;
using Infrastructure.Llm;
using Infrastructure.Vocabulary;

namespace Infrastructure.Backfill;

/// <summary>
/// Backfills a topic's vocabulary-in-context content (passage + target words). Reuses
/// <see cref="LlmVocabularyPassageGenerator"/>'s prompt and parser and mirrors the persist logic
/// of <c>GetVocabularyTopicQueryHandler</c>, so batch-filled content matches a live first-open.
/// </summary>
public sealed class VocabularyContentBackfiller : IContentBackfiller
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly string _model;

    public VocabularyContentBackfiller(IVocabularyTopicRepository topics, HermesGatewayOptions gateway)
    {
        _topics = topics;
        _model = ContentLlm.Model(gateway);
    }

    public string Module => "vocab";

    public Task<BatchContentRequest?> BuildRequestAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        if (!topic.NeedsContentRefresh)
            return Task.FromResult<BatchContentRequest?>(null);

        var request = new BatchContentRequest(
            $"{Module}_{topic.Id}",
            _model,
            LlmVocabularyPassageGenerator.SystemPrompt,
            LlmVocabularyPassageGenerator.BuildUserPrompt(topic.Title, topic.Level, VocabularyTopic.TargetWordCount),
            LlmVocabularyPassageGenerator.MaxOutputTokens);

        return Task.FromResult<BatchContentRequest?>(request);
    }

    public async Task<bool> ApplyResultAsync(VocabularyTopic topic, string responseText, CancellationToken cancellationToken)
    {
        if (!topic.NeedsContentRefresh)
            return false;

        var content = LlmVocabularyPassageGenerator.Parse(responseText);
        if (!content.HasContent)
            return false;

        var words = content.Words
            .Where(w => !string.IsNullOrWhiteSpace(w.Word) && !string.IsNullOrWhiteSpace(w.Translation))
            .Select(w => TopicWord.Create(
                w.Word, w.Translation, w.ExampleSentence, PartOfSpeechParser.Parse(w.Pos)))
            .ToList();
        if (words.Count == 0)
            return false;

        try
        {
            topic.FillContent(content.Passage, words);
            await _topics.SaveAsync(topic, cancellationToken);
            return true;
        }
        catch (DomainException)
        {
            // Generated content failed a domain invariant - leave the topic pending (rules 8, 11).
            return false;
        }
    }
}
