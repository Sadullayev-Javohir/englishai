using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using MediatR;

namespace Application.Identity.GetCurrentUser;

public sealed class GetCurrentUserQueryHandler : IRequestHandler<GetCurrentUserQuery, AuthenticatedUserDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;

    public GetCurrentUserQueryHandler(IUserAccountStore accounts, ILearnerProfileRepository profiles)
    {
        _accounts = accounts;
        _profiles = profiles;
    }

    public async Task<AuthenticatedUserDto> Handle(GetCurrentUserQuery request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Identity.UserAccount), request.UserId);

        // The account id is also the learner id; a profile exists once placement is done or
        // a starting level was chosen, which is exactly what "has onboarded" means.
        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);
        return AuthenticatedUserDto.From(account, profile);
    }
}
