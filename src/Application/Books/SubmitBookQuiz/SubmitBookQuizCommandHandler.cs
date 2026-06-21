using Application.Books.Dtos;
using Application.Books.Ports;
using Application.Common;
using Application.Gamification;
using Application.Learning.Ports;
using Domain.Books;
using Domain.Learning;
using MediatR;

namespace Application.Books.SubmitBookQuiz;

/// <summary>
/// Grades a book section quiz, records the learner's progress (best result per section) and marks
/// the book read once every section is passed. The score also feeds the Reading skill (G.4) when
/// the learner has a profile - reading a book is a reading activity - but a profile is not required
/// for the book library to track completion.
/// </summary>
public sealed class SubmitBookQuizCommandHandler
    : IRequestHandler<SubmitBookQuizCommand, BookQuizResultDto>
{
    private readonly IBookRepository _books;
    private readonly IBookProgressStore _progress;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IDailyProgressRecorder _dailyProgress;
    private readonly TimeProvider _clock;

    public SubmitBookQuizCommandHandler(
        IBookRepository books,
        IBookProgressStore progress,
        ILearnerProfileRepository profiles,
        IDailyProgressRecorder dailyProgress,
        TimeProvider clock)
    {
        _books = books;
        _progress = progress;
        _profiles = profiles;
        _dailyProgress = dailyProgress;
        _clock = clock;
    }

    public async Task<BookQuizResultDto> Handle(
        SubmitBookQuizCommand request, CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(request.BookId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Book), request.BookId);

        var section = book.FindSection(request.SectionId)
                      ?? throw new NotFoundException(nameof(BookSection), request.SectionId);

        if (!section.IsFilled)
            throw new NotFoundException("Book section content", request.SectionId);

        // Last answer wins if the same question appears twice in the submission.
        var answers = request.Answers
            .GroupBy(a => a.QuestionId)
            .ToDictionary(g => g.Key, g => g.Last().SelectedOptionIndex);

        var result = section.GradeQuiz(answers);

        var outcomes = result.Outcomes
            .Select(o => new BookQuestionOutcomeDto(
                o.QuestionId,
                o.SelectedOptionIndex,
                o.CorrectOptionIndex,
                o.IsCorrect,
                o.IsCorrect ? null : o.Explanation))
            .ToList();

        var now = _clock.GetUtcNow();

        // Record per-section progress (keeps the best result; marks the book completed when every
        // section is passed). Only a passing attempt confirms a section, but every attempt updates
        // the best score so the learner sees their progress.
        var progress = await _progress.GetAsync(request.LearnerId, book.Id, cancellationToken)
                       ?? BookProgress.Start(request.LearnerId, book.Id, now);
        progress.RecordSection(section.Id, result.CorrectCount, result.Passed, book.SectionCount, now);
        await _progress.SaveAsync(progress, cancellationToken);

        // Feed the Reading skill score (G.4) when the learner has a profile (mirrors Reading).
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        if (profile is not null)
        {
            profile.RecordActivity(SkillType.Reading, result.ScorePercent, now);
            foreach (var outcome in result.Outcomes.Where(outcome => !outcome.IsCorrect))
            {
                var question = section.Questions.Single(item => item.Id == outcome.QuestionId);
                profile.RecordError(
                    ErrorCategory.Vocabulary,
                    SkillType.Reading,
                    now,
                    "book_quiz",
                    outcome.QuestionId,
                    question.Prompt,
                    AnswerAt(question.Options, outcome.SelectedOptionIndex),
                    AnswerAt(question.Options, outcome.CorrectOptionIndex),
                    outcome.Explanation);
            }
            await _profiles.TrackAsync(profile, cancellationToken);
        }

        await _progress.CommitAsync(cancellationToken);

        if (result.Passed)
        {
            try
            {
                await _dailyProgress.RecordSkillAsync(
                    request.LearnerId, SkillType.Reading, result.ScorePercent, now, cancellationToken);
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception)
            {
            }
        }

        return new BookQuizResultDto(
            book.Id,
            section.Id,
            result.TotalQuestions,
            result.CorrectCount,
            result.RequiredCorrect,
            result.ScorePercent,
            result.Passed,
            progress.IsCompleted,
            progress.PassedSectionCount,
            book.SectionCount,
            outcomes);
    }

    private static string? AnswerAt(IReadOnlyList<string> options, int index) =>
        index >= 0 && index < options.Count ? options[index] : null;
}
