using Application.Identity.Dtos;

namespace Application.Admin.Dtos;

/// <summary>
/// The signed-in user's admin standing, used by the SPA to decide whether to show the admin nav
/// entry / route at all. <see cref="IsAdmin"/> is true for both admins and the super-admin;
/// <see cref="CanManageAdmins"/> is true only for the super-admin (who may promote/demote others).
/// </summary>
public sealed record AdminAccessDto(AdminRole Role)
{
    public bool IsAdmin => Role is AdminRole.Admin or AdminRole.SuperAdmin;

    public bool CanManageAdmins => Role is AdminRole.SuperAdmin;
}
