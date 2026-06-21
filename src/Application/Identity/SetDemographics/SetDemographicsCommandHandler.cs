using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Identity.SetDemographics;

public sealed class SetDemographicsCommandHandler
    : IRequestHandler<SetDemographicsCommand, AuthenticatedUserDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;
    private readonly TimeProvider _clock;

    public SetDemographicsCommandHandler(
        IUserAccountStore accounts,
        ILearnerProfileRepository profiles,
        TimeProvider clock)
    {
        _accounts = accounts;
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<AuthenticatedUserDto> Handle(SetDemographicsCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);
        account.SetDemographics(
            request.BirthDate,
            request.Gender,
            request.AcquisitionSource,
            request.AcquisitionSourceOther,
            _clock.GetUtcNow());
        await _accounts.UpdateAsync(account, cancellationToken);
        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);
        return AuthenticatedUserDto.From(account, profile);
    }
}
