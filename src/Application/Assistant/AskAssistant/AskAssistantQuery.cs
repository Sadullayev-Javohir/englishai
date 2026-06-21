using Application.Assistant.Dtos;
using MediatR;

namespace Application.Assistant.AskAssistant;

public sealed record AskAssistantQuery(string Question, IReadOnlyList<AssistantTurnDto> History)
    : IRequest<AssistantReplyDto>;
