using Application.Common;
using Application.Grammar.Dtos;
using Application.Grammar.Models;
using Application.Grammar.Ports;
using Application.Subscription.Access;
using Application.Vocabulary;
using Application.Vocabulary.Ports;
using Domain.Common;
using Domain.Grammar;
using Domain.Vocabulary;
using MediatR;

namespace Application.Grammar.GetGrammarLesson;

/// <summary>
/// Resolves a topic's grammar lesson, generating and caching it on first open - the same lazy-fill
/// pattern as a reading lesson (<c>GetReadingPassageQueryHandler</c>). The topic's grammar focus is
/// taught in the topic's own context. A failed generation leaves the lesson honestly pending
/// (<c>IsReady=false</c>) rather than fabricating content (rules 8, 11).
/// </summary>
public sealed class GetGrammarLessonQueryHandler : IRequestHandler<GetGrammarLessonQuery, GrammarLessonDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IGrammarRepository _lessons;
    private readonly IGrammarContentGenerator _generator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITopicAccessPolicy _access;

    public GetGrammarLessonQueryHandler(
        IVocabularyTopicRepository topics,
        IGrammarRepository lessons,
        IGrammarContentGenerator generator,
        ICurrentUserAccessor currentUser,
        ITopicAccessPolicy access)
    {
        _topics = topics;
        _lessons = lessons;
        _generator = generator;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<GrammarLessonDto> Handle(GetGrammarLessonQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        // Trial paywall (H.1): topics beyond the free allowance require Premium (server-side enforce).
        if (_currentUser.LearnerId is { } learnerId)
            await _access.EnsureLearningAccessAsync(
                learnerId, topic.Id, Domain.Learning.SkillType.Grammar, cancellationToken);

        var lesson = await _lessons.GetByTopicIdAsync(topic.Id, cancellationToken);

        if (lesson is null || !lesson.IsFilled)
            lesson = await TryGenerateAsync(topic, lesson, cancellationToken);

        return lesson is { IsFilled: true }
            ? GrammarLessonDto.FromDomain(topic, lesson)
            : GrammarLessonDto.Pending(topic);
    }

    // Best-effort lazy fill: generate the five steps and cache them. Any failure leaves the lesson
    // honestly "pending" rather than fabricating content (rules 8, 11).
    private async Task<GrammarLesson?> TryGenerateAsync(
        VocabularyTopic topic, GrammarLesson? existing, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _generator.GenerateAsync(
                topic.Title, topic.GrammarFocusCode, topic.Level,
                TopicTargetWords.Of(topic), cancellationToken);
            if (!content.HasContent)
                return existing;

            var exercises = content.Exercises
                .Where(e => !string.IsNullOrWhiteSpace(e.Prompt) && e.Options.Count >= GrammarExercise.MinOptions
                            && e.CorrectOptionIndex >= 0 && e.CorrectOptionIndex < e.Options.Count)
                .Select(e => GrammarExercise.Create(e.Type, e.Prompt, e.Options, e.CorrectOptionIndex, null, e.Explanation))
                .ToList();
            if (exercises.Count == 0)
                return existing;

            var tasks = content.ApplicationTasks
                .Where(t => !string.IsNullOrWhiteSpace(t.Prompt))
                .Select(t => GrammarApplicationTask.Create(t.TargetSkill, t.Prompt))
                .ToList();

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
            return lesson;
        }
        catch (DomainException)
        {
            // Generated content failed a domain invariant - keep the lesson pending.
            return existing;
        }
    }
}
