using Domain.Common;

namespace Domain.Notifications;

/// <summary>
/// A notification composed by a super-admin and shown to every learner (PROJECT-SPEC Qism A,
/// operator console). Unlike the derived reminders, a broadcast is a single durable row authored by a
/// human operator - so its <see cref="Title"/>/<see cref="Body"/> are exempt from the "no free Uzbek
/// text" rule (docs/development-guide.md rule 11 targets AI-generated text, not an operator's own words). It carries a
/// real <see cref="CreatedAt"/> (the moment it was sent) and an optional in-app destination.
/// </summary>
public sealed class AdminBroadcast
{
    // Parameterless ctor for EF Core materialization.
    private AdminBroadcast()
    {
        Title = null!;
        Body = null!;
    }

    private AdminBroadcast(
        Guid id, string title, string body, string? linkUrl, DateTimeOffset createdAt, Guid createdByUserId)
    {
        Id = id;
        Title = title;
        Body = body;
        LinkUrl = linkUrl;
        CreatedAt = createdAt;
        CreatedByUserId = createdByUserId;
    }

    public Guid Id { get; private set; }

    /// <summary>Short headline shown in bold at the top of the notification.</summary>
    public string Title { get; private set; }

    /// <summary>The message body shown under the title.</summary>
    public string Body { get; private set; }

    /// <summary>Optional in-app destination the broadcast deep-links to when tapped (e.g. <c>/speaking</c>).</summary>
    public string? LinkUrl { get; private set; }

    /// <summary>The moment the broadcast was sent - its genuine arrival time.</summary>
    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>The super-admin account that authored the broadcast (audit trail).</summary>
    public Guid CreatedByUserId { get; private set; }

    public static AdminBroadcast Create(
        string title, string body, string? linkUrl, DateTimeOffset createdAt, Guid createdByUserId)
    {
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Broadcast title must not be empty.");
        if (string.IsNullOrWhiteSpace(body))
            throw new DomainException("Broadcast body must not be empty.");
        if (createdByUserId == Guid.Empty)
            throw new DomainException("Broadcast author must not be empty.");

        var link = string.IsNullOrWhiteSpace(linkUrl) ? null : linkUrl.Trim();
        return new AdminBroadcast(Guid.NewGuid(), title.Trim(), body.Trim(), link, createdAt, createdByUserId);
    }
}
