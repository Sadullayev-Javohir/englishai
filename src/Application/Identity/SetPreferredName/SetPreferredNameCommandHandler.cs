using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Identity.SetPreferredName;

public sealed class SetPreferredNameCommandHandler
    : IRequestHandler<SetPreferredNameCommand, AuthenticatedUserDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;

    public SetPreferredNameCommandHandler(IUserAccountStore accounts, ILearnerProfileRepository profiles)
    {
        _accounts = accounts;
        _profiles = profiles;
    }

    public async Task<AuthenticatedUserDto> Handle(
        SetPreferredNameCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        account.SetPreferredName(request.PreferredName);
        await _accounts.UpdateAsync(account, cancellationToken);

        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);
        return AuthenticatedUserDto.From(account, profile);
    }
}
