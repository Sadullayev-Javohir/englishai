using Application.Assistant.Dtos;
using MediatR;

namespace Application.Assistant.Contextual;

public sealed record AskContextualAssistantQuery(
    string Area,
    string Title,
    string Context,
    string FocusText,
    string Question,
    IReadOnlyList<ContextualAssistantTurnDto> History) : IRequest<ContextualAssistantReplyDto>;
