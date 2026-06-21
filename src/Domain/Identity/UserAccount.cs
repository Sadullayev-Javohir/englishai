using Domain.Common;

namespace Domain.Identity;

/// <summary>
/// A registered user of the platform. The product authenticates exclusively through
/// Google (docs/development-guide.md §3 - Google OAuth 2.0 + JWT), so an account is uniquely identified
/// by the stable Google subject (<c>sub</c>) claim. The account <see cref="Id"/> doubles
/// as the learner id used across every learner-scoped feature, so signing in with the
/// same Google account always resumes the same progress.
/// </summary>
public sealed class UserAccount
{
    // Parameterless ctor for EF Core materialization.
    private UserAccount()
    {
        GoogleSubject = string.Empty;
        Email = string.Empty;
        DisplayName = string.Empty;
    }

    private UserAccount(
        Guid id,
        string googleSubject,
        string email,
        string displayName,
        string? pictureUrl,
        DateTimeOffset now)
    {
        Id = id;
        GoogleSubject = googleSubject;
        Email = email;
        DisplayName = displayName;
        PictureUrl = pictureUrl;
        CreatedAt = now;
        LastLoginAt = now;
        ProTrialExpiresAt = now.AddDays(ProTrialDurationDays);
    }

    public Guid Id { get; private set; }

    /// <summary>Google's stable, unique subject identifier (the <c>sub</c> claim).</summary>
    public string GoogleSubject { get; private set; }

    public string Email { get; private set; }
    public string DisplayName { get; private set; }

    /// <summary>
    /// The name the AI speaking tutor addresses the learner by. The learner provides it once
    /// (a one-time modal asks "what should I call you?"); until then it is null and the tutor is
    /// instructed never to invent a name. Distinct from <see cref="DisplayName"/> (which Google
    /// supplies and may be a full legal name) so the learner controls how the AI greets them.
    /// </summary>
    public string? PreferredName { get; private set; }

    /// <summary>
    /// The user's chosen public handle (normalized lowercase). Null until the user picks one
    /// in the post-sign-up username step - the SPA uses that null to route a brand-new account
    /// into the handle-setup screen before onboarding. Unique across all accounts.
    /// </summary>
    public string? Username { get; private set; }

    /// <summary>URL of the Google profile photo, when the user shared one.</summary>
    public string? PictureUrl { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset LastLoginAt { get; private set; }
    public DateTimeOffset ProTrialExpiresAt { get; private set; }

    public DateOnly? BirthDate { get; private set; }
    public Gender? Gender { get; private set; }
    public AcquisitionSource? AcquisitionSource { get; private set; }
    public string? AcquisitionSourceOther { get; private set; }
    public DateTimeOffset? DemographicsCompletedAt { get; private set; }

    public bool HasCompletedDemographics =>
        BirthDate is not null && Gender is not null && AcquisitionSource is not null &&
        DemographicsCompletedAt is not null;

    /// <summary>
    /// How long a new account gets full access for free.
    ///
    /// Shortened from thirty days: a month of free Pro is a month of variable cost with no decision
    /// at the end of it - long enough that the learner forgets they were ever trialling, and long
    /// enough that a signup in week one is still costing money in week five. A week is enough to
    /// build the habit and short enough to create a moment where the learner has to choose.
    ///
    /// The value is applied at registration, so changing it only affects NEW accounts; existing
    /// learners keep the expiry already written to their row.
    /// </summary>
    public const int ProTrialDurationDays = 7;

    public bool IsProTrialActive(DateTimeOffset now) => ProTrialExpiresAt > now;

    /// <summary>
    /// Whether a super-admin has granted this account access to the admin panel. The super-admin
    /// itself is identified by a built-in email allowlist (resolved outside the domain, like
    /// complimentary access), so it is always an admin regardless of this flag; this flag covers
    /// the additional admins the super-admin promotes. Defaults to false for every new account.
    /// </summary>
    public bool IsAdmin { get; private set; }

    /// <summary>
    /// Registers a brand-new account from a validated Google identity. The id is freshly
    /// generated and thereafter doubles as the learner id.
    /// </summary>
    public static UserAccount Register(
        string googleSubject,
        string email,
        string displayName,
        string? pictureUrl,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(googleSubject))
            throw new DomainException("Google subject must not be empty.");
        if (string.IsNullOrWhiteSpace(email))
            throw new DomainException("Email must not be empty.");

        var name = string.IsNullOrWhiteSpace(displayName) ? email : displayName.Trim();

        return new UserAccount(Guid.NewGuid(), googleSubject.Trim(), email.Trim(), name, pictureUrl, now);
    }

    /// <summary>
    /// Refreshes the mutable profile fields from the latest Google identity and records the
    /// sign-in time. Called on every returning login so the name/photo stay current.
    /// </summary>
    public void RecordLogin(string email, string displayName, string? pictureUrl, DateTimeOffset now)
    {
        if (!string.IsNullOrWhiteSpace(email))
            Email = email.Trim();
        if (!string.IsNullOrWhiteSpace(displayName))
            DisplayName = displayName.Trim();
        if (!HasCustomPicture)
            PictureUrl = pictureUrl;
        LastLoginAt = now;
    }

    public bool HasCustomPicture => PictureUrl?.StartsWith("/api/auth/avatar/", StringComparison.Ordinal) == true;

    public void SetCustomPicture(DateTimeOffset updatedAt) =>
        PictureUrl = $"/api/auth/avatar/{Id}?v={updatedAt.ToUnixTimeMilliseconds()}";

    public void ClearPicture()
    {
        if (HasCustomPicture)
            PictureUrl = null;
    }

    public void RestoreExternalPicture(string? pictureUrl)
    {
        if (HasCustomPicture)
            PictureUrl = pictureUrl;
    }

    /// <summary>
    /// Sets (or changes) the public handle. The value is normalized and validated against the
    /// shared <see cref="UsernameRules"/>; uniqueness is enforced by the calling handler against
    /// the account store (the domain cannot see other aggregates).
    /// </summary>
    public void SetUsername(string username)
    {
        var normalized = UsernameRules.Normalize(username);
        if (!UsernameRules.IsValid(normalized))
            throw new DomainException("Username does not meet the required format.");

        Username = normalized;
    }

    /// <summary>Updates the display name shown across the app (the user may override Google's).</summary>
    public void UpdateDisplayName(string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
            throw new DomainException("Display name must not be empty.");

        DisplayName = displayName.Trim();
    }

    /// <summary>The longest name the AI tutor will store - a guard against an unbounded value.</summary>
    public const int MaxPreferredNameLength = 40;

    /// <summary>
    /// Sets the name the AI tutor uses for the learner (the one-time "what should I call you?"
    /// answer). Trimmed; blank clears it back to "unknown" so the tutor stops using a name. The
    /// value is stored verbatim and is the only name the tutor is ever given - it never invents one.
    /// </summary>
    public void SetPreferredName(string? preferredName)
    {
        var trimmed = preferredName?.Trim();
        if (string.IsNullOrEmpty(trimmed))
        {
            PreferredName = null;
            return;
        }

        if (trimmed.Length > MaxPreferredNameLength)
            throw new DomainException($"Preferred name must be at most {MaxPreferredNameLength} characters.");

        PreferredName = trimmed;
    }

    public void SetDemographics(
        DateOnly birthDate,
        Gender gender,
        AcquisitionSource acquisitionSource,
        string? acquisitionSourceOther,
        DateTimeOffset now)
    {
        var today = DateOnly.FromDateTime(now.UtcDateTime);
        if (birthDate > today || birthDate < today.AddYears(-121))
            throw new DomainException("Birth date must represent an age between 0 and 120.");
        if (!Enum.IsDefined(gender))
            throw new DomainException("Gender is invalid.");
        if (!Enum.IsDefined(acquisitionSource))
            throw new DomainException("Acquisition source is invalid.");

        var other = acquisitionSourceOther?.Trim();
        if (acquisitionSource == Domain.Identity.AcquisitionSource.Other)
        {
            if (string.IsNullOrWhiteSpace(other) || other.Length is < 2 or > 100)
                throw new DomainException("Other acquisition source must be between 2 and 100 characters.");
        }
        else
        {
            other = null;
        }

        BirthDate = birthDate;
        Gender = gender;
        AcquisitionSource = acquisitionSource;
        AcquisitionSourceOther = other;
        DemographicsCompletedAt ??= now;
    }

    /// <summary>
    /// Grants or revokes admin-panel access. Called only by the super-admin promotion flow; the
    /// authorization check (that the caller is the super-admin) lives in the application handler,
    /// which the domain cannot see.
    /// </summary>
    public void SetAdmin(bool isAdmin) => IsAdmin = isAdmin;
}
