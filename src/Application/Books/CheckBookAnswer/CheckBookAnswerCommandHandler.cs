using Application.Books.Dtos;
using Application.Books.Ports;
using Application.Common;
using Domain.Books;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Books.CheckBookAnswer;

public sealed class CheckBookAnswerCommandHandler
    : IRequestHandler<CheckBookAnswerCommand, BookQuestionOutcomeDto>
{
    private readonly IBookRepository _books;

    public CheckBookAnswerCommandHandler(IBookRepository books)
    {
        _books = books;
    }

    public async Task<BookQuestionOutcomeDto> Handle(
        CheckBookAnswerCommand request,
        CancellationToken cancellationToken)
    {
        var book = await _books.GetByIdAsync(request.BookId, cancellationToken)
                   ?? throw new NotFoundException(nameof(Book), request.BookId);
        var section = book.FindSection(request.SectionId)
                      ?? throw new NotFoundException(nameof(BookSection), request.SectionId);
        var question = section.Questions.SingleOrDefault(item => item.Id == request.QuestionId)
                       ?? throw new NotFoundException(nameof(BookQuestion), request.QuestionId);

        if (request.SelectedOptionIndex >= question.Options.Count)
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.SelectedOptionIndex),
                    "Selected option index is out of range."),
            });

        var isCorrect = question.IsCorrect(request.SelectedOptionIndex);
        return new BookQuestionOutcomeDto(
            question.Id,
            request.SelectedOptionIndex,
            question.CorrectOptionIndex,
            isCorrect,
            isCorrect ? null : question.Explanation);
    }
}
