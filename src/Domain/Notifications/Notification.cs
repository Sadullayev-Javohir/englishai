using Domain.Common;

namespace Domain.Notifications;

/// <summary>
/// An in-app notification delivered to a learner (PROJECT-SPEC Qism A, "Bildirishnoma
/// tizimi"). The <see cref="Code"/> identifies the template that produced it and the
/// <see cref="Message"/> is the already-resolved Uzbek text (templates live in the
/// content layer per docs/development-guide.md rule 11 - never free-generated here).
/// </summary>
public sealed class Notification
{
    private Notification()
    {
        Code = null!;
        Message = null!;
    }

    private Notification(Guid learnerId, string code, string message, DateTimeOffset createdAt, string? linkUrl)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Code = code;
        Message = message;
        CreatedAt = createdAt;
        LinkUrl = linkUrl;
        IsRead = false;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }

    /// <summary>The template code that produced this notification (e.g. <c>notify.review_one</c>).</summary>
    public string Code { get; private set; }

    /// <summary>The resolved Uzbek text shown to the learner.</summary>
    public string Message { get; private set; }

    /// <summary>
    /// Optional in-app destination the notification deep-links to when tapped (e.g.
    /// <c>/vocabulary/review</c> for an SRS reminder). Null means it is informational only.
    /// </summary>
    public string? LinkUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public bool IsRead { get; private set; }

    public static Notification Create(
        Guid learnerId, string code, string message, DateTimeOffset createdAt, string? linkUrl = null)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (string.IsNullOrWhiteSpace(code))
            throw new DomainException("Notification code must not be empty.");
        if (string.IsNullOrWhiteSpace(message))
            throw new DomainException("Notification message must not be empty.");

        return new Notification(learnerId, code, message, createdAt, linkUrl);
    }

    public void MarkRead() => IsRead = true;
}
