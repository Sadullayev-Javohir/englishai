using Domain.Support;

namespace Application.Support;

public sealed record SupportAttachmentDto(Guid Id, string FileName, string ContentType, long SizeBytes, string Url);
public sealed record SupportMessageDto(Guid Id, Guid SenderId, SupportSenderKind SenderKind, string? Text,
    DateTimeOffset CreatedAt, bool IsRead, IReadOnlyList<SupportAttachmentDto> Attachments);
public sealed record SupportConversationDto(Guid Id, Guid LearnerId, string LearnerName, string LearnerEmail,
    string? LearnerPictureUrl, Guid? AssignedAdminId, string? AssignedAdminName, SupportConversationStatus Status,
    DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? ClosedAt, int UnreadCount,
    IReadOnlyList<SupportMessageDto> Messages);
public sealed record SupportConversationSummaryDto(Guid Id, Guid LearnerId, string LearnerName, string LearnerEmail,
    string? LearnerPictureUrl, Guid? AssignedAdminId, string? AssignedAdminName, SupportConversationStatus Status,
    DateTimeOffset UpdatedAt, int UnreadCount, string LastMessagePreview);
public sealed record SupportImageUpload(string FileName, string ContentType, byte[] Data);

public interface ISupportConversationRepository
{
    Task<SupportConversation?> GetByLearnerAsync(Guid learnerId, CancellationToken cancellationToken);
    Task<SupportConversation?> GetAsync(Guid conversationId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SupportConversation>> ListAsync(string filter, Guid adminId, int limit, CancellationToken cancellationToken);
    Task AddAsync(SupportConversation conversation, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
}

public interface ISupportService
{
    Task<SupportConversationDto> GetLearnerConversationAsync(Guid learnerId, CancellationToken cancellationToken);
    Task<IReadOnlyList<SupportConversationSummaryDto>> ListAdminAsync(Guid adminId, string filter, int limit, CancellationToken cancellationToken);
    Task<SupportConversationDto> GetAdminConversationAsync(Guid adminId, Guid conversationId, CancellationToken cancellationToken);
    Task<SupportMessageDto> SendLearnerMessageAsync(Guid learnerId, string? text, IReadOnlyList<SupportImageUpload> files, CancellationToken cancellationToken);
    Task<SupportMessageDto> SendAdminMessageAsync(Guid adminId, Guid conversationId, string? text, IReadOnlyList<SupportImageUpload> files, CancellationToken cancellationToken);
    Task<SupportConversationDto> AssignAsync(Guid adminId, Guid conversationId, CancellationToken cancellationToken);
    Task<SupportConversationDto> SetClosedAsync(Guid adminId, Guid conversationId, bool closed, CancellationToken cancellationToken);
    Task MarkReadAsync(Guid readerId, Guid conversationId, bool admin, CancellationToken cancellationToken);
    Task<bool> CanAccessAsync(Guid userId, Guid conversationId, bool admin, CancellationToken cancellationToken);
}

public interface ISupportRealtimeNotifier
{
    Task MessageCreatedAsync(Guid conversationId, Guid learnerId, SupportMessageDto message, CancellationToken cancellationToken);
    Task ConversationUpdatedAsync(Guid conversationId, Guid learnerId, SupportConversationDto conversation, CancellationToken cancellationToken);
    Task MessagesReadAsync(Guid conversationId, Guid learnerId, bool byAdmin, DateTimeOffset readAt, CancellationToken cancellationToken);
}
