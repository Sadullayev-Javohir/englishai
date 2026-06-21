using System.Text.Json;
using Application.Speaking.Ports;
using Domain.Assessment;
using Domain.Curriculum;
using Domain.Speaking;
using Domain.Vocabulary;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Speaking;

public sealed class SpeakingCurriculumProvider : ISpeakingCurriculumProvider
{
    private readonly EnglishAiDbContext _db;

    public SpeakingCurriculumProvider(EnglishAiDbContext db) => _db = db;

    public async Task<SpeakingCurriculumContext?> ForTopicAsync(Guid topicId, CancellationToken cancellationToken)
    {
        var topic = await _db.VocabularyTopics.AsNoTracking().Include(x => x.Words)
            .FirstOrDefaultAsync(x => x.Id == topicId, cancellationToken);
        if (topic is null) return null;
        var blueprint = await _db.TopicSpeakingBlueprints.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TopicId == topicId, cancellationToken);
        return Build(topic, blueprint);
    }

    public async Task<SpeakingCurriculumContext?> MatchAsync(
        CefrLevel level, string text, CancellationToken cancellationToken)
    {
        var terms = Tokenize(text).ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (terms.Count == 0) return null;
        var topics = await _db.VocabularyTopics.AsNoTracking().Include(x => x.Words)
            .Where(x => x.Level == level).OrderBy(x => x.Sequence).ToListAsync(cancellationToken);
        var best = topics.Select(topic => new
            {
                Topic = topic,
                Score = Tokenize($"{topic.Title} {topic.Category} {string.Join(' ', topic.Words.Select(w => w.Word))}")
                    .Count(terms.Contains),
            })
            .OrderByDescending(x => x.Score).ThenBy(x => x.Topic.Sequence).FirstOrDefault();
        if (best is null || best.Score == 0) return null;
        var blueprint = await _db.TopicSpeakingBlueprints.AsNoTracking()
            .FirstOrDefaultAsync(x => x.TopicId == best.Topic.Id, cancellationToken);
        return Build(best.Topic, blueprint);
    }

    private static SpeakingCurriculumContext Build(VocabularyTopic topic, TopicSpeakingBlueprint? blueprint)
    {
        var topicWords = topic.Words.Select(x => x.Word).Take(12).ToList();
        var priorityWords = ParseStrings(blueprint?.PriorityWordsJson);
        return new SpeakingCurriculumContext(
            topic.Id,
            topic.Title,
            blueprint?.Objective,
            blueprint?.PrimaryGrammarFocus ?? topic.GrammarFocusCode,
            blueprint?.ReviewGrammarFocus,
            priorityWords.Count > 0 ? priorityWords : topicWords,
            ParseStrings(blueprint?.QuestionsJson));
    }

    private static IReadOnlyList<string> ParseStrings(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return Array.Empty<string>();
        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Array) return Array.Empty<string>();
            return document.RootElement.EnumerateArray()
                .Select(item => item.ValueKind == JsonValueKind.String ? item.GetString() :
                    item.TryGetProperty("question", out var question) ? question.GetString() : null)
                .Where(item => !string.IsNullOrWhiteSpace(item)).Select(item => item!.Trim()).ToList();
        }
        catch (JsonException) { return Array.Empty<string>(); }
    }

    private static IEnumerable<string> Tokenize(string text) =>
        text.ToLowerInvariant().Split(new[] { ' ', ',', '.', '?', '!', ':', ';', '-', '_', '\'', '"', '(', ')' },
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(word => word.Length > 2);
}
