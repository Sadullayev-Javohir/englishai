using Application.Common;
using Application.Listening.Dtos;
using Application.Listening.Ports;
using Application.Subscription.Access;
using Application.Vocabulary;
using Application.Vocabulary.Ports;
using Domain.Common;
using Domain.Listening;
using Domain.Vocabulary;
using MediatR;

namespace Application.Listening.GetListeningExercise;

/// <summary>
/// Resolves a topic's listening exercise, generating and caching it on first open - the same
/// lazy-fill pattern as a reading lesson (<c>GetReadingPassageQueryHandler</c>). A failed generation
/// leaves the exercise honestly pending (<c>IsReady=false</c>) rather than fabricating content
/// (rules 8, 11).
/// </summary>
public sealed class GetListeningExerciseQueryHandler
    : IRequestHandler<GetListeningExerciseQuery, ListeningExerciseDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IListeningRepository _exercises;
    private readonly IListeningContentGenerator _generator;
    private readonly ICurrentUserAccessor _currentUser;
    private readonly ITopicAccessPolicy _access;

    public GetListeningExerciseQueryHandler(
        IVocabularyTopicRepository topics,
        IListeningRepository exercises,
        IListeningContentGenerator generator,
        ICurrentUserAccessor currentUser,
        ITopicAccessPolicy access)
    {
        _topics = topics;
        _exercises = exercises;
        _generator = generator;
        _currentUser = currentUser;
        _access = access;
    }

    public async Task<ListeningExerciseDto> Handle(
        GetListeningExerciseQuery request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        // Trial paywall (H.1): topics beyond the free allowance require Premium (server-side enforce).
        if (_currentUser.LearnerId is { } learnerId)
            await _access.EnsureLearningAccessAsync(
                learnerId, topic.Id, Domain.Learning.SkillType.Listening, cancellationToken);

        var exercise = await _exercises.GetByTopicIdAsync(topic.Id, cancellationToken);

        if (exercise is null || !exercise.IsFilled)
            exercise = await TryGenerateAsync(topic, exercise, cancellationToken);

        return exercise is { IsFilled: true }
            ? ListeningExerciseDto.FromDomain(topic, exercise)
            : ListeningExerciseDto.Pending(topic);
    }

    // Best-effort lazy fill: generate the transcript + questions and cache them. Any failure leaves
    // the exercise honestly "pending" rather than fabricating content (rules 8, 11).
    private async Task<ListeningExercise?> TryGenerateAsync(
        VocabularyTopic topic, ListeningExercise? existing, CancellationToken cancellationToken)
    {
        try
        {
            var content = await _generator.GenerateAsync(
                topic.Title, topic.Level, TopicTargetWords.Of(topic), cancellationToken);
            if (!content.HasContent)
                return existing;

            var questions = content.Questions
                .Where(q => !string.IsNullOrWhiteSpace(q.Prompt) && q.Options.Count >= ListeningQuestion.MinOptions
                            && q.CorrectOptionIndex >= 0 && q.CorrectOptionIndex < q.Options.Count)
                .Select(q => ListeningQuestion.Create(q.Prompt, q.Options, q.CorrectOptionIndex, null, q.Explanation))
                .ToList();
            if (questions.Count == 0)
                return existing;

            var exercise = existing ?? ListeningExercise.ForTopic(
                topic.Id, topic.Title, topic.Category, topic.Level, DateTimeOffset.UtcNow);
            exercise.FillContent(content.Transcript, questions);
            await _exercises.SaveAsync(exercise, cancellationToken);
            return exercise;
        }
        catch (DomainException)
        {
            // Generated content failed a domain invariant - keep the exercise pending.
            return existing;
        }
    }
}
