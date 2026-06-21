using System.Security.Cryptography;
using System.Text;
using Application.Assistant.Dtos;
using Application.Assistant.Ports;
using Application.Common;
using MediatR;

namespace Application.Assistant.Contextual;

public sealed class AskContextualAssistantQueryHandler(
    IContextualAssistantCoordinator coordinator,
    ICurrentUserAccessor currentUser) : IRequestHandler<AskContextualAssistantQuery, ContextualAssistantReplyDto>
{
    public async Task<ContextualAssistantReplyDto> Handle(AskContextualAssistantQuery request, CancellationToken cancellationToken)
    {
        var history = request.History.TakeLast(6)
            .Select(x => new ContextualAssistantTurn(x.Role.Trim().ToLowerInvariant(), x.Text.Trim())).ToArray();
        var area = request.Area.Trim().ToLowerInvariant();
        var title = request.Title.Trim();
        var context = request.Context.Trim();
        var focus = request.FocusText.Trim();
        var question = request.Question.Trim();
        var normalizedQuestion = string.Join(' ', question.ToLowerInvariant().Split(
            (char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var keySource = new StringBuilder("v2|").Append(area).Append('|').Append(title.ToLowerInvariant())
            .Append('|').Append(context.ToLowerInvariant()).Append('|').Append(focus.ToLowerInvariant())
            .Append('|').Append(normalizedQuestion);
        var cacheKey = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(keySource.ToString().ToLowerInvariant())));
        var reply = await coordinator.ExecuteAsync(
            new ContextualAssistantWork(cacheKey, area, title, context, focus, question, history),
            currentUser.LearnerId?.ToString() ?? "anonymous",
            cancellationToken);
        return new ContextualAssistantReplyDto(string.IsNullOrWhiteSpace(reply) ? null : reply.Trim());
    }
}
