using Application.Common;
using Application.Gamification;
using Application.Learning.Ports;
using Application.Listening.Dtos;
using Application.Subscription.Access;
using Application.Listening.Ports;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Learning;
using Domain.Vocabulary;
using MediatR;

namespace Application.Listening.SubmitListeningQuiz;

public sealed class SubmitListeningQuizCommandHandler
    : IRequestHandler<SubmitListeningQuizCommand, ListeningQuizResultDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IListeningRepository _exercises;
    private readonly IListeningContentProvider _content;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ITopicCompletionStore _completions;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly ITopicAccessPolicy _access;
    private readonly TimeProvider _clock;

    public SubmitListeningQuizCommandHandler(
        IVocabularyTopicRepository topics,
        IListeningRepository exercises,
        IListeningContentProvider content,
        ILearnerProfileRepository profiles,
        ITopicCompletionStore completions,
        IDailyProgressRecorder dailyProgress,
        ITopicAccessPolicy access,
        TimeProvider clock)
    {
        _topics = topics;
        _exercises = exercises;
        _content = content;
        _profiles = profiles;
        _completions = completions;
        _dailyProgress = dailyProgress;
        _access = access;
        _clock = clock;
    }

    public async Task<ListeningQuizResultDto> Handle(
        SubmitListeningQuizCommand request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);
        await _access.EnsureLearningAccessAsync(
            request.LearnerId, topic.Id, SkillType.Listening, cancellationToken);

        var exercise = await _exercises.GetByTopicIdAsync(topic.Id, cancellationToken)
                       ?? throw new NotFoundException("Listening exercise", request.TopicId);

        // Last answer wins if the same question appears twice in the submission.
        var answers = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().SelectedOptionIndex);

        var result = exercise.GradeQuiz(answers);

        // The "why" note, shown only for a wrong answer: the generated English explanation when the
        // question carries one (the immersion path), otherwise the vetted Uzbek hint (rule 11).
        var outcomes = result.Outcomes
            .Select(o => new ListeningQuestionOutcomeDto(
                o.QuestionId,
                o.SelectedOptionIndex,
                o.CorrectOptionIndex,
                o.IsCorrect,
                o.IsCorrect ? null : o.Explanation ?? _content.GetHint(o.HintCode)))
            .ToList();

        var now = _clock.GetUtcNow();

        // Feed the Listening skill score (G.4). If the learner has no profile yet (e.g. hasn't
        // finished placement) we skip silently - listening should not require a profile.
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        if (profile is not null)
        {
            profile.RecordActivity(SkillType.Listening, result.ScorePercent, now);
            foreach (var outcome in result.Outcomes.Where(outcome => !outcome.IsCorrect))
            {
                var question = exercise.Questions.Single(item => item.Id == outcome.QuestionId);
                profile.RecordError(
                    ErrorCategory.Vocabulary,
                    SkillType.Listening,
                    now,
                    "listening_quiz",
                    outcome.QuestionId,
                    question.Prompt,
                    AnswerAt(question.Options, outcome.SelectedOptionIndex),
                    AnswerAt(question.Options, outcome.CorrectOptionIndex),
                    outcome.Explanation ?? _content.GetHint(outcome.HintCode));
            }
            // Staged, not committed: the profile and the topic record must land together or not at
            // all, so a failure between them cannot leave half of this submission durable.
            await _profiles.TrackAsync(profile, cancellationToken);
        }

        // Credit the score toward this topic's Listening module (K.5). The store keeps the best
        // score per module, so re-attempting can only raise it. A profile is not required.
        var record = await _completions.GetAsync(request.LearnerId, topic.Id, cancellationToken)
                     ?? TopicCompletionRecord.Start(request.LearnerId, topic.Id, topic.Level, now);
        record.RecordModule(SkillType.Listening, result.ScorePercent, now);
        await _completions.TrackAsync(record, cancellationToken);

        // One commit for everything this submission changed.
        await _completions.CommitAsync(cancellationToken);

        // The learner's result is durable from here on, so a reward failure must not fail the request.
        if (result.Passed)
        {
            await LearningRewards.AwardBestEffortAsync(
                () => _dailyProgress.RecordSkillAsync(
                    request.LearnerId, SkillType.Listening, result.ScorePercent, now, cancellationToken),
                cancellationToken);
        }

        return new ListeningQuizResultDto(
            topic.Id, result.TotalQuestions, result.CorrectCount,
            result.ScorePercent, result.Passed, outcomes, TopicCompletionDto.FromDomain(record));
    }

    private static string? AnswerAt(IReadOnlyList<string> options, int index) =>
        index >= 0 && index < options.Count ? options[index] : null;
}
