using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Support;
using Application.Storage;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Notifications.Dtos;
using Application.Notifications.Ports;
using Domain.Notifications;
using Domain.Support;
using FluentValidation;

namespace Web.Endpoints;

public static class SupportEndpoints
{
    public static IEndpointRouteBuilder MapSupportEndpoints(this IEndpointRouteBuilder app)
    {
        var learner = app.MapGroup("/api/support").WithTags("Support");
        learner.MapGet("/conversation", async (HttpContext http, ISupportService service, CancellationToken ct) =>
            Resolve(http.User) is { } userId
                ? Results.Ok(await service.GetLearnerConversationAsync(userId, ct))
                : Results.Unauthorized());
        learner.MapPost("/messages", async (HttpRequest request, HttpContext http, ISupportService service,
            ISupportRealtimeNotifier realtime, IUserAccountStore accounts, IAdminAuthorization admins,
            INotificationDispatcher notifications, INotificationRealtimeNotifier notificationRealtime,
            CancellationToken ct) =>
        {
            var userId = Resolve(http.User);
            if (userId is null) return Results.Unauthorized();
            var (text, files) = await ReadMessageAsync(request, ct);
            var message = await service.SendLearnerMessageAsync(userId.Value, text, files, ct);
            var conversation = await service.GetLearnerConversationAsync(userId.Value, ct);
            await realtime.MessageCreatedAsync(conversation.Id, conversation.LearnerId, message, ct);
            await realtime.ConversationUpdatedAsync(conversation.Id, conversation.LearnerId, conversation, ct);
            await NotifyAdminsAsync(conversation, message, accounts, admins, notifications, notificationRealtime, ct);
            return Results.Ok(message);
        }).DisableAntiforgery();
        learner.MapPost("/read", async (MarkReadRequest body, HttpContext http, ISupportService service,
            ISupportRealtimeNotifier realtime, TimeProvider clock, CancellationToken ct) =>
        {
            var userId = Resolve(http.User);
            if (userId is null) return Results.Unauthorized();
            await service.MarkReadAsync(userId.Value, body.ConversationId, false, ct);
            await realtime.MessagesReadAsync(body.ConversationId, userId.Value, false, clock.GetUtcNow(), ct);
            return Results.NoContent();
        });
        learner.MapGet("/attachments/{attachmentId:guid}", async (Guid attachmentId, HttpContext http,
            ISupportConversationRepository conversations, IObjectStorage storage, CancellationToken ct) =>
        {
            var userId = Resolve(http.User);
            if (userId is null) return Results.Unauthorized();
            var conversation = await conversations.GetByLearnerAsync(userId.Value, ct);
            var attachment = conversation?.Messages.SelectMany(x => x.Attachments).SingleOrDefault(x => x.Id == attachmentId);
            return attachment is null ? Results.NotFound() : await OpenAttachmentAsync(attachment, storage, http, ct);
        });

        var admin = app.MapGroup("/api/admin/support").WithTags("Admin Support");
        admin.MapGet("/conversations", async (string? filter, int? limit, HttpContext http,
            ISupportService service, CancellationToken ct) => Resolve(http.User) is { } adminId
                ? Results.Ok(await service.ListAdminAsync(adminId, filter ?? "open", limit ?? 50, ct))
                : Results.Unauthorized());
        admin.MapGet("/conversations/{conversationId:guid}", async (Guid conversationId, HttpContext http,
            ISupportService service, CancellationToken ct) => Resolve(http.User) is { } adminId
                ? Results.Ok(await service.GetAdminConversationAsync(adminId, conversationId, ct))
                : Results.Unauthorized());
        admin.MapPost("/conversations/{conversationId:guid}/messages", async (Guid conversationId,
            HttpRequest request, HttpContext http, ISupportService service, ISupportRealtimeNotifier realtime,
            INotificationDispatcher notifications, INotificationRealtimeNotifier notificationRealtime,
            CancellationToken ct) =>
        {
            var adminId = Resolve(http.User);
            if (adminId is null) return Results.Unauthorized();
            var (text, files) = await ReadMessageAsync(request, ct);
            var message = await service.SendAdminMessageAsync(adminId.Value, conversationId, text, files, ct);
            var conversation = await service.GetAdminConversationAsync(adminId.Value, conversationId, ct);
            await realtime.MessageCreatedAsync(conversation.Id, conversation.LearnerId, message, ct);
            await realtime.ConversationUpdatedAsync(conversation.Id, conversation.LearnerId, conversation, ct);
            await NotifyAsync(conversation.LearnerId, message, "Support javobi: ", "/support",
                notifications, notificationRealtime, ct);
            return Results.Ok(message);
        }).DisableAntiforgery();
        admin.MapPost("/conversations/{conversationId:guid}/assign", async (Guid conversationId, HttpContext http,
            ISupportService service, ISupportRealtimeNotifier realtime, CancellationToken ct) =>
        {
            var adminId = Resolve(http.User);
            if (adminId is null) return Results.Unauthorized();
            var conversation = await service.AssignAsync(adminId.Value, conversationId, ct);
            await realtime.ConversationUpdatedAsync(conversation.Id, conversation.LearnerId, conversation, ct);
            return Results.Ok(conversation);
        });
        admin.MapPost("/conversations/{conversationId:guid}/status", async (Guid conversationId,
            SetStatusRequest body, HttpContext http, ISupportService service, ISupportRealtimeNotifier realtime,
            CancellationToken ct) =>
        {
            var adminId = Resolve(http.User);
            if (adminId is null) return Results.Unauthorized();
            var conversation = await service.SetClosedAsync(adminId.Value, conversationId, body.Closed, ct);
            await realtime.ConversationUpdatedAsync(conversation.Id, conversation.LearnerId, conversation, ct);
            return Results.Ok(conversation);
        });
        admin.MapPost("/conversations/{conversationId:guid}/read", async (Guid conversationId, HttpContext http,
            ISupportService service, ISupportRealtimeNotifier realtime, TimeProvider clock, CancellationToken ct) =>
        {
            var adminId = Resolve(http.User);
            if (adminId is null) return Results.Unauthorized();
            var conversation = await service.GetAdminConversationAsync(adminId.Value, conversationId, ct);
            await service.MarkReadAsync(adminId.Value, conversationId, true, ct);
            await realtime.MessagesReadAsync(conversationId, conversation.LearnerId, true, clock.GetUtcNow(), ct);
            return Results.NoContent();
        });
        admin.MapGet("/attachments/{attachmentId:guid}", async (Guid attachmentId, HttpContext http,
            ISupportConversationRepository conversations, Application.Common.IAdminAuthorization authorization,
            IObjectStorage storage, CancellationToken ct) =>
        {
            var adminId = Resolve(http.User);
            if (adminId is null) return Results.Unauthorized();
            if (await authorization.GetRoleAsync(adminId.Value, ct) is Application.Identity.Dtos.AdminRole.None)
                return Results.Forbid();
            var conversation = (await conversations.ListAsync("open", adminId.Value, 100, ct))
                .Concat(await conversations.ListAsync("closed", adminId.Value, 100, ct))
                .FirstOrDefault(x => x.Messages.SelectMany(message => message.Attachments).Any(x => x.Id == attachmentId));
            var attachment = conversation?.Messages.SelectMany(x => x.Attachments).SingleOrDefault(x => x.Id == attachmentId);
            return attachment is null ? Results.NotFound() : await OpenAttachmentAsync(attachment, storage, http, ct);
        });
        return app;
    }

    private static async Task NotifyAdminsAsync(
        SupportConversationDto conversation,
        SupportMessageDto message,
        IUserAccountStore accounts,
        IAdminAuthorization admins,
        INotificationDispatcher notifications,
        INotificationRealtimeNotifier realtime,
        CancellationToken cancellationToken)
    {
        foreach (var account in await accounts.GetAllAsync(cancellationToken))
        {
            if (await admins.GetRoleAsync(account.Id, cancellationToken) is AdminRole.None)
                continue;

            await NotifyAsync(account.Id, message, $"{conversation.LearnerName}: ",
                $"/admin/support?conversation={conversation.Id}", notifications, realtime, cancellationToken);
        }
    }

    private static async Task NotifyAsync(
        Guid recipientId,
        SupportMessageDto message,
        string messagePrefix,
        string linkUrl,
        INotificationDispatcher notifications,
        INotificationRealtimeNotifier realtime,
        CancellationToken cancellationToken)
    {
        var content = string.IsNullOrWhiteSpace(message.Text) ? "Rasm yuborildi" : message.Text.Trim();
        var body = $"{messagePrefix}{content}";
        var notification = Notification.Create(recipientId, $"support.message.{message.Id:N}",
            body, message.CreatedAt, linkUrl);
        await notifications.SendAsync(notification, cancellationToken);
        var dto = NotificationDto.FromDomain(notification);
        await realtime.NotificationSentAsync(recipientId, dto, cancellationToken);
    }

    private static async Task<IResult> OpenAttachmentAsync(
        SupportAttachment attachment, IObjectStorage storage, HttpContext http, CancellationToken cancellationToken)
    {
        var stored = await storage.OpenReadAsync(attachment.ObjectKey, ObjectVisibility.Private, cancellationToken);
        if (stored is null) return Results.NotFound();
        http.Response.Headers.CacheControl = "private, no-store";
        return Results.Stream(stored.Content, attachment.ContentType, enableRangeProcessing: true);
    }

    private static async Task<(string? Text, IReadOnlyList<SupportImageUpload> Files)> ReadMessageAsync(
        HttpRequest request, CancellationToken cancellationToken)
    {
        if (!request.HasFormContentType) throw new ValidationException("Support messages must use multipart form data.");
        var form = await request.ReadFormAsync(cancellationToken);
        if (form.Files.Count > 4) throw new ValidationException("A support message can contain at most four images.");
        var files = new List<SupportImageUpload>(form.Files.Count);
        foreach (var file in form.Files)
        {
            if (file.Length > 10 * 1024 * 1024) throw new ValidationException("Each support image must be 10 MB or smaller.");
            await using var stream = new MemoryStream();
            await file.CopyToAsync(stream, cancellationToken);
            files.Add(new SupportImageUpload(file.FileName, file.ContentType.ToLowerInvariant(), stream.ToArray()));
        }
        return (form["text"].FirstOrDefault(), files);
    }

    private static Guid? Resolve(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public sealed record MarkReadRequest(Guid ConversationId);
    public sealed record SetStatusRequest(bool Closed);
}
