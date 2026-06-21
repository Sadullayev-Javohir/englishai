using System.Security.Cryptography;
using Application.Common;
using Application.Storage;
using Domain.Identity;
using Domain.Support;
using Application.Identity.Ports;
using FluentValidation;

namespace Application.Support;

public sealed class SupportService(
    ISupportConversationRepository conversations,
    IUserAccountStore accounts,
    IAdminAuthorization admins,
    IObjectStorage storage,
    TimeProvider clock) : ISupportService
{
    private const int MaxFiles = 4;
    private const int MaxFileBytes = 10 * 1024 * 1024;

    public async Task<SupportConversationDto> GetLearnerConversationAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByLearnerAsync(learnerId, cancellationToken);
        if (conversation is null)
        {
            conversation = SupportConversation.Create(learnerId, clock.GetUtcNow());
            await conversations.AddAsync(conversation, cancellationToken);
            await conversations.SaveChangesAsync(cancellationToken);
        }
        return await MapAsync(conversation, SupportSenderKind.Learner, cancellationToken);
    }

    public async Task<IReadOnlyList<SupportConversationSummaryDto>> ListAdminAsync(
        Guid adminId, string filter, int limit, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(adminId, cancellationToken);
        var items = await conversations.ListAsync(filter, adminId, Math.Clamp(limit, 1, 100), cancellationToken);
        var accountIds = items.Select(item => item.LearnerId)
            .Concat(items.Where(item => item.AssignedAdminId.HasValue).Select(item => item.AssignedAdminId!.Value))
            .Distinct()
            .ToArray();
        var accountsById = (await accounts.GetManyByIdsAsync(accountIds, cancellationToken))
            .ToDictionary(account => account.Id);
        var result = new List<SupportConversationSummaryDto>(items.Count);
        foreach (var item in items)
        {
            accountsById.TryGetValue(item.LearnerId, out var learner);
            var assigned = item.AssignedAdminId.HasValue && accountsById.TryGetValue(item.AssignedAdminId.Value, out var account)
                ? account : null;
            var last = item.Messages.OrderByDescending(x => x.CreatedAt).FirstOrDefault();
            result.Add(new SupportConversationSummaryDto(item.Id, item.LearnerId,
                learner?.DisplayName ?? "Foydalanuvchi", learner?.Email ?? string.Empty, learner?.PictureUrl,
                item.AssignedAdminId, assigned?.DisplayName, item.Status, item.UpdatedAt,
                UnreadCount(item, SupportSenderKind.Admin),
                last?.Text ?? (last?.Attachments.Count > 0 ? "Rasm yuborildi" : "Yangi suhbat")));
        }
        return result;
    }

    public async Task<SupportConversationDto> GetAdminConversationAsync(
        Guid adminId, Guid conversationId, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(adminId, cancellationToken);
        return await MapAsync(await RequiredAsync(conversationId, cancellationToken), SupportSenderKind.Admin, cancellationToken);
    }

    public async Task<SupportMessageDto> SendLearnerMessageAsync(
        Guid learnerId, string? text, IReadOnlyList<SupportImageUpload> files, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetByLearnerAsync(learnerId, cancellationToken);
        if (conversation is null)
        {
            conversation = SupportConversation.Create(learnerId, clock.GetUtcNow());
            await conversations.AddAsync(conversation, cancellationToken);
        }
        return await AddMessageAsync(conversation, learnerId, SupportSenderKind.Learner, text, files, cancellationToken);
    }

    public async Task<SupportMessageDto> SendAdminMessageAsync(
        Guid adminId, Guid conversationId, string? text, IReadOnlyList<SupportImageUpload> files, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(adminId, cancellationToken);
        var conversation = await RequiredAsync(conversationId, cancellationToken);
        if (conversation.AssignedAdminId != adminId)
            throw new ForbiddenException("Support conversation must be assigned to you before replying.");
        return await AddMessageAsync(conversation, adminId, SupportSenderKind.Admin, text, files, cancellationToken);
    }

    public async Task<SupportConversationDto> AssignAsync(Guid adminId, Guid conversationId, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(adminId, cancellationToken);
        var conversation = await RequiredAsync(conversationId, cancellationToken);
        conversation.Assign(adminId, clock.GetUtcNow());
        await conversations.SaveChangesAsync(cancellationToken);
        return await MapAsync(conversation, SupportSenderKind.Admin, cancellationToken);
    }

    public async Task<SupportConversationDto> SetClosedAsync(
        Guid adminId, Guid conversationId, bool closed, CancellationToken cancellationToken)
    {
        await EnsureAdminAsync(adminId, cancellationToken);
        var conversation = await RequiredAsync(conversationId, cancellationToken);
        if (closed) conversation.Close(clock.GetUtcNow()); else conversation.Reopen(clock.GetUtcNow());
        await conversations.SaveChangesAsync(cancellationToken);
        return await MapAsync(conversation, SupportSenderKind.Admin, cancellationToken);
    }

    public async Task MarkReadAsync(Guid readerId, Guid conversationId, bool admin, CancellationToken cancellationToken)
    {
        var conversation = await RequiredAsync(conversationId, cancellationToken);
        if (admin) await EnsureAdminAsync(readerId, cancellationToken);
        else if (conversation.LearnerId != readerId) throw new ForbiddenException("You can only access your own support chat.");
        conversation.MarkRead(admin ? SupportSenderKind.Admin : SupportSenderKind.Learner, clock.GetUtcNow());
        await conversations.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> CanAccessAsync(Guid userId, Guid conversationId, bool admin, CancellationToken cancellationToken)
    {
        var conversation = await conversations.GetAsync(conversationId, cancellationToken);
        if (conversation is null) return false;
        if (!admin) return conversation.LearnerId == userId;
        return await admins.GetRoleAsync(userId, cancellationToken) is not Application.Identity.Dtos.AdminRole.None;
    }

    private async Task<SupportMessageDto> AddMessageAsync(SupportConversation conversation, Guid senderId,
        SupportSenderKind senderKind, string? text, IReadOnlyList<SupportImageUpload> files,
        CancellationToken cancellationToken)
    {
        if (files.Count > MaxFiles) throw new ValidationException("A support message can contain at most four images.");
        if (string.IsNullOrWhiteSpace(text) && files.Count == 0) throw new ValidationException("Message text or an image is required.");
        var drafts = new List<SupportAttachmentDraft>(files.Count);
        try
        {
            foreach (var file in files)
            {
                ValidateFile(file);
                var checksum = Convert.ToHexString(SHA256.HashData(file.Data)).ToLowerInvariant();
                var attachmentId = Guid.NewGuid();
                var extension = Extension(file.ContentType);
                var key = $"support/{conversation.LearnerId:N}/{conversation.Id:N}/{attachmentId:N}-{checksum[..16]}{extension}";
                await using var stream = new MemoryStream(file.Data, writable: false);
                await storage.PutAsync(new ObjectWriteRequest(key, stream, file.ContentType, file.Data.LongLength,
                    checksum, ObjectVisibility.Private, "private, no-store"), cancellationToken);
                drafts.Add(new SupportAttachmentDraft(attachmentId, key, SanitizeFileName(file.FileName),
                    file.ContentType, file.Data.LongLength, checksum));
            }
            var message = conversation.AddMessage(senderId, senderKind, text, drafts, clock.GetUtcNow());
            await conversations.SaveChangesAsync(cancellationToken);
            return MapMessage(conversation, message, senderKind);
        }
        catch
        {
            foreach (var draft in drafts)
                try { await storage.DeleteAsync(draft.ObjectKey, ObjectVisibility.Private, cancellationToken); } catch { }
            throw;
        }
    }

    private async Task<SupportConversationDto> MapAsync(
        SupportConversation conversation, SupportSenderKind viewer, CancellationToken cancellationToken)
    {
        var learner = await accounts.GetByIdAsync(conversation.LearnerId, cancellationToken);
        var assigned = conversation.AssignedAdminId.HasValue
            ? await accounts.GetByIdAsync(conversation.AssignedAdminId.Value, cancellationToken) : null;
        var messages = new List<SupportMessageDto>(conversation.Messages.Count);
        foreach (var message in conversation.Messages.OrderBy(x => x.CreatedAt))
            messages.Add(MapMessage(conversation, message, viewer));
        return new SupportConversationDto(conversation.Id, conversation.LearnerId,
            learner?.DisplayName ?? "Foydalanuvchi", learner?.Email ?? string.Empty, learner?.PictureUrl,
            conversation.AssignedAdminId, assigned?.DisplayName, conversation.Status, conversation.CreatedAt,
            conversation.UpdatedAt, conversation.ClosedAt, UnreadCount(conversation, viewer), messages);
    }

    private static SupportMessageDto MapMessage(
        SupportConversation conversation, SupportMessage message, SupportSenderKind viewer)
    {
        var prefix = viewer == SupportSenderKind.Admin ? "/api/admin/support/attachments/" : "/api/support/attachments/";
        var attachments = message.Attachments.Select(attachment => new SupportAttachmentDto(
            attachment.Id, attachment.FileName, attachment.ContentType, attachment.SizeBytes, $"{prefix}{attachment.Id}")).ToList();
        var readAt = message.SenderKind == SupportSenderKind.Learner
            ? conversation.AdminLastReadAt : conversation.LearnerLastReadAt;
        return new SupportMessageDto(message.Id, message.SenderId, message.SenderKind, message.Text,
            message.CreatedAt, readAt >= message.CreatedAt, attachments);
    }

    private async Task<SupportConversation> RequiredAsync(Guid id, CancellationToken cancellationToken) =>
        await conversations.GetAsync(id, cancellationToken) ?? throw new NotFoundException(nameof(SupportConversation), id);

    private async Task EnsureAdminAsync(Guid adminId, CancellationToken cancellationToken)
    {
        if (await admins.GetRoleAsync(adminId, cancellationToken) is Application.Identity.Dtos.AdminRole.None)
            throw new ForbiddenException("Admin access is required.");
    }

    private static int UnreadCount(SupportConversation conversation, SupportSenderKind viewer)
    {
        var readAt = viewer == SupportSenderKind.Admin ? conversation.AdminLastReadAt : conversation.LearnerLastReadAt;
        return conversation.Messages.Count(x => x.SenderKind != viewer && (!readAt.HasValue || x.CreatedAt > readAt.Value));
    }

    private static void ValidateFile(SupportImageUpload file)
    {
        if (file.Data.Length is <= 0 or > MaxFileBytes) throw new ValidationException("Each support image must be 10 MB or smaller.");
        var detected = DetectImage(file.Data);
        if (!string.Equals(detected, file.ContentType, StringComparison.OrdinalIgnoreCase))
            throw new ValidationException("Support image content does not match its file type.");
    }

    private static string DetectImage(ReadOnlySpan<byte> data)
    {
        if (data.Length >= 3 && data[0] == 0xff && data[1] == 0xd8 && data[2] == 0xff) return "image/jpeg";
        if (data.Length >= 8 && data[..8].SequenceEqual(new byte[] { 0x89, 0x50, 0x4e, 0x47, 0x0d, 0x0a, 0x1a, 0x0a })) return "image/png";
        if (data.Length >= 12 && data[..4].SequenceEqual("RIFF"u8) && data[8..12].SequenceEqual("WEBP"u8)) return "image/webp";
        if (data.Length >= 6 && (data[..6].SequenceEqual("GIF87a"u8) || data[..6].SequenceEqual("GIF89a"u8))) return "image/gif";
        throw new ValidationException("Only PNG, JPEG, WebP, and GIF images are supported.");
    }

    private static string Extension(string contentType) => contentType switch
    { "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", "image/gif" => ".gif", _ => string.Empty };
    private static string SanitizeFileName(string value) => string.IsNullOrWhiteSpace(value) ? "screenshot" : Path.GetFileName(value.Trim());
}
