using Application.Listening.Dtos;
using MediatR;

namespace Application.Listening.CheckListeningAnswer;

public sealed record CheckListeningAnswerQuery(
    Guid TopicId,
    Guid QuestionId,
    int SelectedOptionIndex) : IRequest<ListeningAnswerCheckDto>;
