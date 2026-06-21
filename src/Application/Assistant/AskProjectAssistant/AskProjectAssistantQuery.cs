using Application.Assistant.Dtos;
using MediatR;

namespace Application.Assistant.AskProjectAssistant;

public sealed record AskProjectAssistantQuery(string Question, IReadOnlyList<AssistantTurnDto> History, string Locale = "uz")
    : IRequest<AssistantReplyDto>;
