using Application.Reading.Dtos;
using MediatR;

namespace Application.Reading.CheckReadingAnswer;

public sealed record CheckReadingAnswerQuery(
    Guid TopicId,
    Guid QuestionId,
    int SelectedOptionIndex) : IRequest<ReadingAnswerCheckDto>;
