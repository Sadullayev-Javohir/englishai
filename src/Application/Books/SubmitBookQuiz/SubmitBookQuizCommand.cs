using Application.Books.Dtos;
using MediatR;

namespace Application.Books.SubmitBookQuiz;

/// <summary>One answer in a book section quiz submission.</summary>
public sealed record BookQuizAnswer(Guid QuestionId, int SelectedOptionIndex);

/// <summary>
/// Submits a learner's answers to a book section's comprehension quiz for server-side grading.
/// At 70% correct the section is marked read; when that completes the book, it is confirmed read.
/// The score is also recorded as Reading skill activity (G.4) when the learner has
/// a profile.
/// </summary>
public sealed record SubmitBookQuizCommand(
    Guid BookId,
    Guid SectionId,
    Guid LearnerId,
    IReadOnlyList<BookQuizAnswer> Answers) : IRequest<BookQuizResultDto>;
