using Application.Support;
using Microsoft.AspNetCore.SignalR;
using Web.Hubs;

namespace Web.Support;

public sealed class SignalRSupportNotifier(IHubContext<SupportHub> hub) : ISupportRealtimeNotifier
{
    public async Task MessageCreatedAsync(Guid conversationId, Guid learnerId, SupportMessageDto message, CancellationToken ct)
    {
        await Task.WhenAll(
            hub.Clients.Group(SupportHub.ConversationGroup(conversationId)).SendAsync(SupportHub.MessageCreatedEvent, message, ct),
            hub.Clients.Group(SupportHub.LearnerGroup(learnerId)).SendAsync(SupportHub.MessageCreatedEvent, message, ct),
            hub.Clients.Group(SupportHub.AdminGroup).SendAsync(SupportHub.MessageCreatedEvent, new { conversationId, message }, ct));
    }

    public async Task ConversationUpdatedAsync(Guid conversationId, Guid learnerId, SupportConversationDto conversation, CancellationToken ct)
    {
        await Task.WhenAll(
            hub.Clients.Group(SupportHub.ConversationGroup(conversationId)).SendAsync(SupportHub.ConversationUpdatedEvent, conversation, ct),
            hub.Clients.Group(SupportHub.LearnerGroup(learnerId)).SendAsync(SupportHub.ConversationUpdatedEvent, conversation, ct),
            hub.Clients.Group(SupportHub.AdminGroup).SendAsync(SupportHub.ConversationUpdatedEvent, conversation, ct));
    }

    public async Task MessagesReadAsync(Guid conversationId, Guid learnerId, bool byAdmin, DateTimeOffset readAt, CancellationToken ct)
    {
        var payload = new { conversationId, byAdmin, readAt };
        await Task.WhenAll(
            hub.Clients.Group(SupportHub.ConversationGroup(conversationId)).SendAsync(SupportHub.MessagesReadEvent, payload, ct),
            hub.Clients.Group(SupportHub.LearnerGroup(learnerId)).SendAsync(SupportHub.MessagesReadEvent, payload, ct),
            hub.Clients.Group(SupportHub.AdminGroup).SendAsync(SupportHub.MessagesReadEvent, payload, ct));
    }
}
