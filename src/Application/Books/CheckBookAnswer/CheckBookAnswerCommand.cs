using Application.Books.Dtos;
using MediatR;

namespace Application.Books.CheckBookAnswer;

public sealed record CheckBookAnswerCommand(
    Guid BookId,
    Guid SectionId,
    Guid QuestionId,
    int SelectedOptionIndex) : IRequest<BookQuestionOutcomeDto>;
