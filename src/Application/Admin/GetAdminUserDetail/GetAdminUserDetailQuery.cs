using Application.Admin.Dtos;
using MediatR;

namespace Application.Admin.GetAdminUserDetail;

public sealed record GetAdminUserDetailQuery(Guid RequestingUserId, Guid TargetUserId) : IRequest<AdminUserDetailDto>;
