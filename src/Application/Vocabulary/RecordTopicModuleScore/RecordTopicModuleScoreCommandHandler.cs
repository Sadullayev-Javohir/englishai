using Application.Common;
using Application.Gamification;
using Application.Gamification.Dtos;
using Application.Subscription.Access;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.RecordTopicModuleScore;

public sealed class RecordTopicModuleScoreCommandHandler
    : IRequestHandler<RecordTopicModuleScoreCommand, RecordTopicModuleScoreResult>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly ITopicCompletionStore _completions;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly ITopicAccessPolicy _access;
    private readonly ITopicVocabularyEnrollmentService _topicVocabularyEnrollment;
    private readonly TimeProvider _clock;

    public RecordTopicModuleScoreCommandHandler(
        IVocabularyTopicRepository topics,
        ITopicCompletionStore completions,
        IDailyProgressRecorder dailyProgress,
        ITopicAccessPolicy access,
        ITopicVocabularyEnrollmentService topicVocabularyEnrollment,
        TimeProvider clock)
    {
        _topics = topics;
        _completions = completions;
        _dailyProgress = dailyProgress;
        _access = access;
        _topicVocabularyEnrollment = topicVocabularyEnrollment;
        _clock = clock;
    }

    public async Task<RecordTopicModuleScoreResult> Handle(
        RecordTopicModuleScoreCommand request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
            ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        // Trial paywall (H.1): a learner cannot record progress on a topic past their free allowance.
        await _access.EnsureLearningAccessAsync(
            request.LearnerId, topic.Id, request.Module, cancellationToken);

        var now = _clock.GetUtcNow();
        var record = await _completions.GetAsync(request.LearnerId, request.TopicId, cancellationToken)
            ?? TopicCompletionRecord.Start(request.LearnerId, request.TopicId, topic.Level, now);

        var justMastered = record.RecordModule(request.Module, request.Score, now);
        await _completions.SaveAsync(record, cancellationToken);

        // The score is durable from here on. The reward lives in Redis and enrollment in another
        // table, so a failure in either must not discard a module score the learner has earned - it
        // reports no reward instead, and the learner keeps their progress.
        var reward = request.Score >= TopicCompletionRecord.MasteryThreshold
            ? await LearningRewards.AwardBestEffortAsync(
                () => _dailyProgress.RecordSkillAsync(
                    request.LearnerId, request.Module, request.Score, now, cancellationToken),
                SkillRewardDto.NotAwarded(),
                cancellationToken)
            : SkillRewardDto.NotAwarded();

        // Enrollment skips words already taken from this topic, so a retry re-enrolls what a failure
        // here missed.
        if (request.Module == Domain.Learning.SkillType.Vocabulary)
        {
            await LearningRewards.AwardBestEffortAsync(
                () => _topicVocabularyEnrollment.EnrollAsync(request.LearnerId, topic, now, cancellationToken),
                cancellationToken);
        }

        return new RecordTopicModuleScoreResult(TopicCompletionDto.FromDomain(record), justMastered, reward);
    }
}
