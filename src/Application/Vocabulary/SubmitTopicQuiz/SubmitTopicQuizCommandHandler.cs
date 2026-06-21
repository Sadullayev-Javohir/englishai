using Application.Common;
using Application.Gamification;
using Application.Subscription.Access;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Learning;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.SubmitTopicQuiz;

public sealed class SubmitTopicQuizCommandHandler
    : IRequestHandler<SubmitTopicQuizCommand, TopicQuizResultDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly ITopicCompletionStore _completions;
    private readonly ITopicVocabularyEnrollmentService _topicVocabularyEnrollment;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly ITopicAccessPolicy _access;
    private readonly TimeProvider _clock;

    public SubmitTopicQuizCommandHandler(
        IVocabularyTopicRepository topics,
        ITopicCompletionStore completions,
        ITopicVocabularyEnrollmentService topicVocabularyEnrollment,
        IDailyProgressRecorder dailyProgress,
        ITopicAccessPolicy access,
        TimeProvider clock)
    {
        _topics = topics;
        _completions = completions;
        _topicVocabularyEnrollment = topicVocabularyEnrollment;
        _dailyProgress = dailyProgress;
        _access = access;
        _clock = clock;
    }

    public async Task<TopicQuizResultDto> Handle(
        SubmitTopicQuizCommand request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
            ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);

        var result = topic.GradeQuiz(request.Answers);
        var dto = TopicQuizResultDto.FromDomain(result);

        // Credit the quiz percentage toward the topic's Vocabulary module (K.5). Only when a
        // learner is identified - anonymous grading leaves no completion record.
        if (request.LearnerId is { } learnerId && learnerId != Guid.Empty)
        {
            await _access.EnsureLearningAccessAsync(
                learnerId, topic.Id, SkillType.Vocabulary, cancellationToken);

            var now = _clock.GetUtcNow();
            var record = await _completions.GetAsync(learnerId, topic.Id, cancellationToken)
                ?? TopicCompletionRecord.Start(learnerId, topic.Id, topic.Level, now);

            record.RecordModule(SkillType.Vocabulary, result.ScorePercent, now);
            await _completions.SaveAsync(record, cancellationToken);

            // The score is durable from here on. Everything below writes to a DIFFERENT store, so a
            // failure there must not turn a graded quiz into a 500 the learner has to re-submit.
            if (result.ScorePercent >= TopicCompletionRecord.MasteryThreshold)
            {
                await LearningRewards.AwardBestEffortAsync(
                    () => _dailyProgress.RecordSkillAsync(
                        learnerId, SkillType.Vocabulary, result.ScorePercent, now, cancellationToken),
                    cancellationToken);
            }

            // Finishing the topic's exercises adds its words to "Mening so'zlarim" (the SRS queue),
            // so they surface in the 3/7/21-kun review later - no manual "Lug'atga qo'shish" step.
            // Enrollment skips words already taken from this topic, so a retry (or simply opening the
            // topic again) re-enrolls whatever a failure here missed.
            await LearningRewards.AwardBestEffortAsync(
                () => _topicVocabularyEnrollment.EnrollAsync(learnerId, topic, now, cancellationToken),
                cancellationToken);

            dto = dto with { Completion = TopicCompletionDto.FromDomain(record) };
        }

        return dto;
    }

}
