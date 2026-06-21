using Application.Ai;
using Application.Common;
using Application.Subscription.Access;
using Application.Vocabulary;
using Application.Vocabulary.Ports;
using Application.Writing.Dtos;
using Application.Writing.Ports;
using Domain.Common;
using Domain.Vocabulary;
using Domain.Writing;
using MediatR;

namespace Application.Writing.GetWritingTask;

/// <summary>
/// Resolves a topic's writing task, generating and caching its prompt on first open - the same
/// lazy-fill pattern as a reading lesson (<c>GetReadingPassageQueryHandler</c>). Provider failures
/// propagate as retryable AI errors rather than being disguised as a permanently pending task.
/// </summary>
public sealed class GetWritingTaskQueryHandler : IRequestHandler<GetWritingTaskQuery, WritingTaskDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IWritingTaskRepository _tasks;
    private readonly IWritingPromptGenerator _generator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITopicAccessPolicy _access;
    private readonly IAiFeatureScope _aiScope;

    public GetWritingTaskQueryHandler(
        IVocabularyTopicRepository topics,
        IWritingTaskRepository tasks,
        IWritingPromptGenerator generator,
        ICurrentUserAccessor currentUser,
        ITopicAccessPolicy access,
        IAiFeatureScope? aiScope = null)
    {
        _topics = topics;
        _tasks = tasks;
        _generator = generator;
        _currentUser = currentUser;
        _access = access;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
    }

    public async Task<WritingTaskDto> Handle(GetWritingTaskQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        // Trial paywall (H.1): topics beyond the free allowance require Premium (server-side enforce).
        if (_currentUser.LearnerId is { } learnerId)
            await _access.EnsureLearningAccessAsync(
                learnerId, topic.Id, Domain.Learning.SkillType.Writing, cancellationToken);

        var task = await _tasks.GetByTopicIdAsync(topic.Id, cancellationToken);

        if (task is null || !task.IsFilled)
            task = await TryGenerateAsync(topic, task, cancellationToken);

        var (min, max) = WritingWordRange.For(topic.Level);
        return task is { IsFilled: true }
            ? WritingTaskDto.FromDomain(topic, task)
            : WritingTaskDto.Pending(topic, min, max);
    }

    // Lazy fill: generate the prompt/guidance and cache them. Provider failures propagate so the
    // client can show an actionable retry state instead of polling an unchanged pending task.
    private async Task<WritingTask?> TryGenerateAsync(
        VocabularyTopic topic, WritingTask? existing, CancellationToken cancellationToken)
    {
        using var scope = await _aiScope.EnterAsync(AiFeature.WritingGeneration, _currentUser.LearnerId, cancellationToken);
        var content = await _generator.GenerateAsync(
            topic.Title, topic.Level, TopicTargetWords.Of(topic), cancellationToken);
        if (!content.HasContent)
            throw new AiAdmissionException(
                "provider_unavailable",
                "AI provider returned no usable writing task.",
                5,
                503);

        var (min, max) = WritingWordRange.For(topic.Level);
        var task = existing ?? WritingTask.ForTopic(topic.Id, topic.Level, min, max, DateTimeOffset.UtcNow);
        task.FillContent(content.Prompt, content.Guidance);
        await _tasks.SaveAsync(task, cancellationToken);
        return task;
    }
}
