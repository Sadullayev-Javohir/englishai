namespace Application.Identity.Dtos;

/// <summary>
/// An account's level of access to the operator admin panel. <see cref="None"/> is an ordinary
/// learner; <see cref="Admin"/> can view the panel; <see cref="SuperAdmin"/> can additionally
/// promote/demote other accounts to admin. The super-admin is fixed by a built-in email allowlist
/// (so the grant survives any data change), while ordinary admins are toggled by the super-admin.
/// </summary>
public enum AdminRole
{
    None = 0,
    Admin = 1,
    SuperAdmin = 2,
}
