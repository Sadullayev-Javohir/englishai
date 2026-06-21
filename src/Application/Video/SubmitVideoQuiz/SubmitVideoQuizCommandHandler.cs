using Application.Common;
using Application.Gamification;
using Application.Gamification.Dtos;
using Application.Video.Models;
using Application.Video.Dtos;
using Application.Video.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Video.SubmitVideoQuiz;

public sealed class SubmitVideoQuizCommandHandler
    : IRequestHandler<SubmitVideoQuizCommand, VideoQuizResultDto>
{
    private readonly IVideoRepository _videos;
    private readonly IVideoContentProvider _content;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly TimeProvider _clock;
    private readonly IVideoQuizStore? _quizStore;

    public SubmitVideoQuizCommandHandler(
        IVideoRepository videos,
        IVideoContentProvider content,
        IDailyProgressRecorder dailyProgress,
        TimeProvider clock,
        IVideoQuizStore? quizStore = null)
    {
        _videos = videos;
        _content = content;
        _dailyProgress = dailyProgress;
        _clock = clock;
        _quizStore = quizStore;
    }

    public async Task<VideoQuizResultDto> Handle(
        SubmitVideoQuizCommand request, CancellationToken cancellationToken)
    {
        if (request.QuizId is { } quizId)
            return await GradeGeneratedQuiz(request, quizId, cancellationToken);
        var lesson = await _videos.GetByIdAsync(request.VideoLessonId, cancellationToken)
                     ?? throw new NotFoundException("Video lesson", request.VideoLessonId);

        // Last answer wins if the same question appears twice in the submission.
        var answers = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().SelectedOptionIndex);

        var result = lesson.GradeQuiz(answers);

        if (result.Passed)
            await _dailyProgress.RecordSkillAsync(
                request.LearnerId, SkillType.Listening, result.ScorePercent, _clock.GetUtcNow(), cancellationToken);

        // Resolve the vetted Uzbek hint (rule 11), shown only when the answer was wrong.
        var outcomes = result.Outcomes
            .Select(o => new QuestionOutcomeDto(
                o.QuestionId,
                o.SelectedOptionIndex,
                o.CorrectOptionIndex,
                o.IsCorrect,
                o.IsCorrect ? null : _content.GetHint(o.HintCode)))
            .ToList();

        return new VideoQuizResultDto(
            result.VideoLessonId, result.TotalQuestions, result.CorrectCount,
            result.ScorePercent, result.Passed, outcomes);
    }

    private async Task<VideoQuizResultDto> GradeGeneratedQuiz(
        SubmitVideoQuizCommand request, Guid quizId, CancellationToken cancellationToken)
    {
        var quiz = _quizStore is null ? null : await _quizStore.GetAsync(quizId, cancellationToken);
        if (quiz is null || quiz.ExpiresAt <= _clock.GetUtcNow())
            throw new VideoQuizUnavailableException("quiz_expired");
        if (quiz.LearnerId != request.LearnerId || quiz.VideoLessonId != request.VideoLessonId)
            throw new ForbiddenException("Quiz belongs to another learner or video.");
        // An already-graded session is immutable, including rewards; retries cannot change answers.
        if (quiz.Result is not null) return quiz.Result;
        if (request.Answers.Count != quiz.Questions.Count ||
            request.Answers.Select(a => a.QuestionId).Distinct().Count() != quiz.Questions.Count ||
            request.Answers.Any(a => a.SelectedOptionIndex is < 0 or > 3 || !quiz.Questions.Any(q => q.Id == a.QuestionId)))
            throw new FluentValidation.ValidationException("Submit exactly one valid answer for every quiz question.");
        var answers = request.Answers.ToDictionary(a => a.QuestionId, a => a.SelectedOptionIndex);
        var outcomes = quiz.Questions.Select(q => new QuestionOutcomeDto(
            q.Id, answers[q.Id], q.CorrectOptionIndex, answers[q.Id] == q.CorrectOptionIndex, q.ExplanationUz)).ToArray();
        var correct = outcomes.Count(o => o.IsCorrect);
        var percent = (int)Math.Round(100d * correct / outcomes.Length);
        var passed = percent >= Domain.Video.VideoQuizResult.PassThresholdPercent;
        var reward = passed
            ? await LearningRewards.AwardBestEffortAsync(
                () => _dailyProgress.RecordSkillAsync(request.LearnerId, SkillType.Listening, percent, _clock.GetUtcNow(), cancellationToken),
                SkillRewardDto.NotAwarded(), cancellationToken)
            : SkillRewardDto.NotAwarded();
        var result = new VideoQuizResultDto(quiz.VideoLessonId, outcomes.Length, correct, percent, passed, outcomes, reward?.AwardedXp ?? 0);
        await _quizStore!.SaveAsync(quiz with { Result = result }, cancellationToken);
        return result;
    }
}
