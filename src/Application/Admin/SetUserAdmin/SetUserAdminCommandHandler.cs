using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Admin.SetUserAdmin;

/// <summary>
/// Applies a super-admin's promote/demote of another account. Authorizes the caller as a super-admin
/// (403 otherwise), then flips the target's <see cref="UserAccount.IsAdmin"/> flag. A super-admin
/// account (its role comes from the email allowlist, not this flag) cannot be demoted here - the
/// allowlist is the single source of truth for that level (docs/development-guide.md §13).
/// </summary>
public sealed class SetUserAdminCommandHandler : IRequestHandler<SetUserAdminCommand, SetUserAdminResultDto>
{
    private readonly IAdminAuthorization _admin;
    private readonly IUserAccountStore _accounts;

    public SetUserAdminCommandHandler(IAdminAuthorization admin, IUserAccountStore accounts)
    {
        _admin = admin;
        _accounts = accounts;
    }

    public async Task<SetUserAdminResultDto> Handle(SetUserAdminCommand request, CancellationToken cancellationToken)
    {
        var viewerRole = await _admin.GetRoleAsync(request.RequestingUserId, cancellationToken);
        if (viewerRole is not AdminRole.SuperAdmin)
            throw new ForbiddenException("Only a super-admin can change admin access.");

        var target = await _accounts.GetByIdAsync(request.TargetUserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.TargetUserId);

        // The super-admin grant is fixed by the email allowlist; the per-account flag can't override it.
        if (_admin.IsSuperAdminEmail(target.Email))
            throw new ConflictException("A super-admin's access cannot be changed.");

        target.SetAdmin(request.IsAdmin);
        await _accounts.UpdateAsync(target, cancellationToken);

        return new SetUserAdminResultDto(target.Id, request.IsAdmin ? AdminRole.Admin : AdminRole.None);
    }
}
