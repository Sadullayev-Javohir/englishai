using Application.Common;
using Application.Gamification;
using Application.Grammar.Dtos;
using Application.Grammar.Ports;
using Application.Learning.Ports;
using Application.Subscription.Access;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using MediatR;

namespace Application.Grammar.SubmitGrammarExercises;

public sealed class SubmitGrammarExercisesCommandHandler
    : IRequestHandler<SubmitGrammarExercisesCommand, GrammarExerciseResultDto>
{
    private readonly IVocabularyTopicRepository _topics;
    private readonly IGrammarRepository _lessons;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ITopicCompletionStore _completions;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly ITopicAccessPolicy _access;
    private readonly TimeProvider _clock;

    public SubmitGrammarExercisesCommandHandler(
        IVocabularyTopicRepository topics,
        IGrammarRepository lessons,
        ILearnerProfileRepository profiles,
        ITopicCompletionStore completions,
        IDailyProgressRecorder dailyProgress,
        ITopicAccessPolicy access,
        TimeProvider clock)
    {
        _topics = topics;
        _lessons = lessons;
        _profiles = profiles;
        _completions = completions;
        _dailyProgress = dailyProgress;
        _access = access;
        _clock = clock;
    }

    public async Task<GrammarExerciseResultDto> Handle(
        SubmitGrammarExercisesCommand request, CancellationToken cancellationToken)
    {
        var topic = await _topics.GetByIdAsync(request.TopicId, cancellationToken)
                    ?? throw new NotFoundException(nameof(VocabularyTopic), request.TopicId);
        await _access.EnsureLearningAccessAsync(
            request.LearnerId, topic.Id, SkillType.Grammar, cancellationToken);

        var lesson = await _lessons.GetByTopicIdAsync(topic.Id, cancellationToken)
                     ?? throw new NotFoundException("Grammar lesson", request.TopicId);

        // Last answer wins if the same exercise appears twice in the submission.
        var exercises = lesson.Exercises.ToDictionary(exercise => exercise.Id);
        var answers = request.Answers
            .GroupBy(a => a.ExerciseId)
            .ToDictionary(g => g.Key, g => g.Last() is { TextAnswer: not null } answer && exercises.TryGetValue(g.Key, out var exercise)
                ? exercise.MatchTextAnswer(answer.TextAnswer)
                : g.Last().SelectedOptionIndex);

        var result = lesson.GradeExercises(answers);

        // The English explanation (immersion teaching note) is shown only when the answer was wrong.
        var outcomes = result.Outcomes
            .Select(o => new GrammarExerciseOutcomeDto(
                o.ExerciseId,
                o.SelectedOptionIndex,
                o.CorrectOptionIndex,
                o.IsCorrect,
                o.IsCorrect ? null : o.Explanation))
            .ToList();

        var now = _clock.GetUtcNow();

        // Feed the Grammar skill score (G.4) and the error heatmap (C.7). If the learner has no
        // profile yet (e.g. hasn't finished placement) we skip silently - grammar practice should
        // not require a profile.
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        if (profile is not null)
        {
            profile.RecordActivity(SkillType.Grammar, result.ScorePercent, now);

            // Every wrong answer is one observation of this topic's error category, so the heatmap
            // reflects the grammar areas the learner struggles with (G.2 ↔ C.7).
            var wrongCount = result.Outcomes.Count(o => !o.IsCorrect);
            for (var i = 0; i < wrongCount; i++)
                profile.RecordError(lesson.Category, SkillType.Grammar, now);

            // Staged, not committed: the profile and the topic record must land together or not at
            // all, so a failure between them cannot leave half of this submission durable.
            await _profiles.TrackAsync(profile, cancellationToken);
        }

        // Credit the score toward this topic's Grammar module (K.5). The store keeps the best score
        // per module, so re-attempting can only raise it. A profile is not required (a topic's
        // mastery checklist is tracked independently, like vocabulary/reading/writing).
        var record = await _completions.GetAsync(request.LearnerId, topic.Id, cancellationToken)
                     ?? TopicCompletionRecord.Start(request.LearnerId, topic.Id, topic.Level, now);
        record.RecordModule(SkillType.Grammar, result.ScorePercent, now);
        await _completions.TrackAsync(record, cancellationToken);

        // One commit for everything this submission changed.
        await _completions.CommitAsync(cancellationToken);

        // The learner's result is durable from here on, so a reward failure must not fail the request.
        if (result.Passed)
        {
            await LearningRewards.AwardBestEffortAsync(
                () => _dailyProgress.RecordSkillAsync(
                    request.LearnerId, SkillType.Grammar, result.ScorePercent, now, cancellationToken),
                cancellationToken);
        }

        return new GrammarExerciseResultDto(
            topic.Id, result.TotalExercises, result.CorrectCount,
            result.ScorePercent, result.Passed, outcomes, TopicCompletionDto.FromDomain(record));
    }
}
