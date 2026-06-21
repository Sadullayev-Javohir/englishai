using Application.Identity.Dtos;
using MediatR;

namespace Application.Admin.SetUserAdmin;

/// <summary>
/// Promotes or demotes another account's admin grant. Only the super-admin may issue it:
/// <see cref="RequestingUserId"/> (from the session, not the body) is authorized as a super-admin and
/// everyone else is rejected with a 403. <see cref="TargetUserId"/> is the account being changed; it is
/// deliberately not named <c>LearnerId</c> so the ownership pipeline behavior leaves it alone (a
/// super-admin acts on accounts other than their own). Returns the target's resulting role.
/// </summary>
public sealed record SetUserAdminCommand(Guid RequestingUserId, Guid TargetUserId, bool IsAdmin)
    : IRequest<SetUserAdminResultDto>;

/// <summary>The target account's id and its effective role after the change.</summary>
public sealed record SetUserAdminResultDto(Guid Id, AdminRole Role);
