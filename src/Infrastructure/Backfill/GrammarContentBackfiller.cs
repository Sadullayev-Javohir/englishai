using Application.Backfill.Models;
using Application.Backfill.Ports;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Application.Vocabulary;
using Domain.Common;
using Domain.Grammar;
using Domain.Vocabulary;
using Infrastructure.Grammar;
using Infrastructure.Llm;

namespace Infrastructure.Backfill;

/// <summary>
/// Backfills a topic's 5-step grammar lesson (the grammar focus taught in the topic's context).
/// Reuses <see cref="LlmGrammarContentGenerator"/>'s prompt and parser and mirrors the persist
/// logic of <c>GetGrammarLessonQueryHandler</c>.
/// </summary>
public sealed class GrammarContentBackfiller : IContentBackfiller
{
    private readonly IGrammarRepository _lessons;
    private readonly IGrammarContentProvider _content;
    private readonly string _model;

    public GrammarContentBackfiller(IGrammarRepository lessons, IGrammarContentProvider content, HermesGatewayOptions gateway)
    {
        _lessons = lessons;
        _content = content;
        _model = ContentLlm.Model(gateway);
    }

    public string Module => "grammar";

    public async Task<BatchContentRequest?> BuildRequestAsync(VocabularyTopic topic, CancellationToken cancellationToken)
    {
        var existing = await _lessons.GetByTopicIdAsync(topic.Id, cancellationToken);
        if (existing is { IsFilled: true })
            return null;

        return new BatchContentRequest(
            $"{Module}_{topic.Id}",
            _model,
            LlmGrammarContentGenerator.SystemPrompt,
            LlmGrammarContentGenerator.BuildUserPrompt(
                topic.Title, topic.GrammarFocusCode, topic.Level, TopicTargetWords.Of(topic)),
            LlmGrammarContentGenerator.MaxOutputTokens);
    }

    public async Task<bool> ApplyResultAsync(VocabularyTopic topic, string responseText, CancellationToken cancellationToken)
    {
        var content = LlmGrammarContentGenerator.Parse(responseText, _content);
        if (!content.HasContent)
            return false;

        var exercises = content.Exercises
            .Where(e => !string.IsNullOrWhiteSpace(e.Prompt) && e.Options.Count >= GrammarExercise.MinOptions
                        && e.CorrectOptionIndex >= 0 && e.CorrectOptionIndex < e.Options.Count)
            .Select(e => GrammarExercise.Create(e.Type, e.Prompt, e.Options, e.CorrectOptionIndex, null, e.Explanation))
            .ToList();
        if (exercises.Count == 0)
            return false;

        var tasks = content.ApplicationTasks
            .Where(t => !string.IsNullOrWhiteSpace(t.Prompt))
            .Select(t => GrammarApplicationTask.Create(t.TargetSkill, t.Prompt))
            .ToList();

        try
        {
            var existing = await _lessons.GetByTopicIdAsync(topic.Id, cancellationToken);
            if (existing is { IsFilled: true })
                return false;

            var examples = content.Examples
                .Where(e => !string.IsNullOrWhiteSpace(e.English))
                .Select(e => GrammarExample.Create(e.English, e.Uzbek))
                .ToList();

            var mistakes = content.CommonMistakesUz
                .Where(m => !string.IsNullOrWhiteSpace(m))
                .Select(m => GrammarCommonMistake.Create(m!))
                .ToList();

            var category = GrammarFocusCategory.For(topic.GrammarFocusCode);
            var lesson = existing ?? GrammarLesson.ForTopic(
                topic.Id, topic.Title, category, topic.Level, topic.GrammarFocusCode, DateTimeOffset.UtcNow);
            lesson.FillContent(content.ContextIntro, content.RuleExplanation, examples, mistakes, exercises, tasks);
            await _lessons.SaveAsync(lesson, cancellationToken);
            return true;
        }
        catch (DomainException)
        {
            return false;
        }
    }
}
