using Domain.Common;

namespace Domain.Support;

public enum SupportConversationStatus { Open = 0, Closed = 1 }
public enum SupportSenderKind { Learner = 0, Admin = 1 }

public sealed class SupportConversation
{
    private readonly List<SupportMessage> _messages = new();
    private SupportConversation() { }
    private SupportConversation(Guid learnerId, DateTimeOffset now)
    {
        Id = Guid.NewGuid(); LearnerId = learnerId; Status = SupportConversationStatus.Open;
        CreatedAt = now; UpdatedAt = now; LearnerLastReadAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public Guid? AssignedAdminId { get; private set; }
    public SupportConversationStatus Status { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public DateTimeOffset? ClosedAt { get; private set; }
    public DateTimeOffset? LearnerLastReadAt { get; private set; }
    public DateTimeOffset? AdminLastReadAt { get; private set; }
    public IReadOnlyList<SupportMessage> Messages => _messages;

    public static SupportConversation Create(Guid learnerId, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty) throw new DomainException("Support conversation needs a learner id.");
        return new SupportConversation(learnerId, now);
    }

    public SupportMessage AddMessage(Guid senderId, SupportSenderKind senderKind, string? text,
        IReadOnlyCollection<SupportAttachmentDraft> attachments, DateTimeOffset now)
    {
        var normalizedText = string.IsNullOrWhiteSpace(text) ? null : text.Trim();
        if (senderId == Guid.Empty) throw new DomainException("Support message needs a sender id.");
        if (normalizedText?.Length > 8000) throw new DomainException("Support message is too long.");
        if (normalizedText is null && attachments.Count == 0) throw new DomainException("Support message needs text or an image.");
        if (attachments.Count > 4) throw new DomainException("A support message can contain at most four images.");
        if (senderKind == SupportSenderKind.Learner && Status == SupportConversationStatus.Closed) Reopen(now);
        var message = SupportMessage.Create(Id, senderId, senderKind, normalizedText, attachments, now);
        _messages.Add(message); UpdatedAt = now;
        if (senderKind == SupportSenderKind.Learner) LearnerLastReadAt = now; else AdminLastReadAt = now;
        return message;
    }

    public void Assign(Guid adminId, DateTimeOffset now)
    {
        if (adminId == Guid.Empty) throw new DomainException("Support assignment needs an admin id.");
        AssignedAdminId = adminId; UpdatedAt = now;
    }

    public void Close(DateTimeOffset now) { Status = SupportConversationStatus.Closed; ClosedAt = now; UpdatedAt = now; }
    public void Reopen(DateTimeOffset now) { Status = SupportConversationStatus.Open; ClosedAt = null; UpdatedAt = now; }
    public void MarkRead(SupportSenderKind reader, DateTimeOffset now)
    { if (reader == SupportSenderKind.Learner) LearnerLastReadAt = now; else AdminLastReadAt = now; }
}

public sealed record SupportAttachmentDraft(Guid Id, string ObjectKey, string FileName, string ContentType, long SizeBytes, string Checksum);

public sealed class SupportMessage
{
    private readonly List<SupportAttachment> _attachments = new();
    private SupportMessage() { }
    public Guid Id { get; private set; }
    public Guid ConversationId { get; private set; }
    public Guid SenderId { get; private set; }
    public SupportSenderKind SenderKind { get; private set; }
    public string? Text { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public IReadOnlyList<SupportAttachment> Attachments => _attachments;

    internal static SupportMessage Create(Guid conversationId, Guid senderId, SupportSenderKind senderKind,
        string? text, IReadOnlyCollection<SupportAttachmentDraft> attachments, DateTimeOffset now)
    {
        var message = new SupportMessage { Id = Guid.NewGuid(), ConversationId = conversationId,
            SenderId = senderId, SenderKind = senderKind, Text = text, CreatedAt = now };
        foreach (var attachment in attachments) message._attachments.Add(SupportAttachment.Create(message.Id, attachment));
        return message;
    }
}

public sealed class SupportAttachment
{
    private SupportAttachment() { }
    public Guid Id { get; private set; }
    public Guid MessageId { get; private set; }
    public string ObjectKey { get; private set; } = string.Empty;
    public string FileName { get; private set; } = string.Empty;
    public string ContentType { get; private set; } = string.Empty;
    public long SizeBytes { get; private set; }
    public string Checksum { get; private set; } = string.Empty;
    internal static SupportAttachment Create(Guid messageId, SupportAttachmentDraft draft) => new()
    { Id = draft.Id, MessageId = messageId, ObjectKey = draft.ObjectKey,
      FileName = draft.FileName[..Math.Min(draft.FileName.Length, 255)], ContentType = draft.ContentType,
      SizeBytes = draft.SizeBytes, Checksum = draft.Checksum };
}
