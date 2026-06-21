using FluentValidation;

namespace Application.Notifications.SendAdminBroadcast;

/// <summary>
/// Guards a super-admin broadcast: an author, a non-empty title and body within sane limits, and - if
/// a destination is given - either one of the app's known in-app routes OR a well-formed absolute
/// http(s) link (super-admins are trusted operators, so an external URL is allowed; it renders as a
/// "Havolani ochish" button in the feed). The 200-char cap matches the LinkUrl column.
/// </summary>
public sealed class SendAdminBroadcastCommandValidator : AbstractValidator<SendAdminBroadcastCommand>
{
    public const int MaxTitleLength = 120;
    public const int MaxBodyLength = 500;

    /// <summary>Matches the <c>AdminBroadcast.LinkUrl</c> column length (Postgres would reject longer).</summary>
    public const int MaxLinkUrlLength = 200;

    /// <summary>The in-app destinations a broadcast may deep-link to (mirrors the reminder routes).</summary>
    public static readonly IReadOnlyList<string> AllowedLinkUrls = new[]
    {
        NotificationCodes.HomeLinkUrl,
        NotificationCodes.VocabularyLinkUrl,
        NotificationCodes.GrammarLinkUrl,
        NotificationCodes.WritingLinkUrl,
        NotificationCodes.SpeakingLinkUrl,
        NotificationCodes.ListeningLinkUrl,
        NotificationCodes.ReadingLinkUrl,
        NotificationCodes.ReviewLinkUrl,
    };

    public SendAdminBroadcastCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();

        RuleFor(x => x.Title)
            .NotEmpty()
            .MaximumLength(MaxTitleLength);

        RuleFor(x => x.Body)
            .NotEmpty()
            .MaximumLength(MaxBodyLength);

        // Optional: a destination is either a known in-app route (deep-links inside the app) or a valid
        // absolute http(s) URL (opens as an external link button). Anything else is rejected so a
        // malformed value can never reach the feed or overflow the LinkUrl column.
        RuleFor(x => x.LinkUrl)
            .Must(BeAValidDestination)
            .WithMessage("Destination must be a known app section or a valid http(s) link.");
    }

    private static bool BeAValidDestination(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
            return true;

        var trimmed = url.Trim();
        if (trimmed.Length > MaxLinkUrlLength)
            return false;

        if (AllowedLinkUrls.Contains(trimmed))
            return true;

        // An external link: must be an absolute http/https URL (never javascript:, mailto:, etc.).
        return Uri.TryCreate(trimmed, UriKind.Absolute, out var uri)
               && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps);
    }
}
