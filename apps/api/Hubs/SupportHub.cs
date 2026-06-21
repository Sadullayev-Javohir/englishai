using Application.Common;
using Application.Support;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace Web.Hubs;

[Authorize]
public sealed class SupportHub(ICurrentUserAccessor currentUser, IAdminAuthorization admins, ISupportService support) : Hub
{
    public const string MessageCreatedEvent = "SupportMessageCreated";
    public const string ConversationUpdatedEvent = "SupportConversationUpdated";
    public const string TypingChangedEvent = "SupportTypingChanged";
    public const string MessagesReadEvent = "SupportMessagesRead";
    public static string ConversationGroup(Guid id) => $"support:conversation:{id:N}";
    public static string LearnerGroup(Guid id) => $"support:learner:{id:N}";
    public const string AdminGroup = "support:admins";

    public override async Task OnConnectedAsync()
    {
        var userId = ResourceOwnership.RequireCurrentLearner(currentUser);
        await Groups.AddToGroupAsync(Context.ConnectionId, LearnerGroup(userId));
        if (await admins.GetRoleAsync(userId, Context.ConnectionAborted) is not Application.Identity.Dtos.AdminRole.None)
            await Groups.AddToGroupAsync(Context.ConnectionId, AdminGroup);
        await base.OnConnectedAsync();
    }

    public async Task JoinConversation(Guid conversationId, bool admin)
    {
        var userId = ResourceOwnership.RequireCurrentLearner(currentUser);
        if (!await support.CanAccessAsync(userId, conversationId, admin, Context.ConnectionAborted))
            throw new HubException("Support conversation access denied.");
        await Groups.AddToGroupAsync(Context.ConnectionId, ConversationGroup(conversationId));
    }

    public async Task SetTyping(Guid conversationId, bool admin, bool typing)
    {
        var userId = ResourceOwnership.RequireCurrentLearner(currentUser);
        if (!await support.CanAccessAsync(userId, conversationId, admin, Context.ConnectionAborted))
            throw new HubException("Support conversation access denied.");
        await Clients.OthersInGroup(ConversationGroup(conversationId))
            .SendAsync(TypingChangedEvent, new { conversationId, senderId = userId, admin, typing }, Context.ConnectionAborted);
    }
}
