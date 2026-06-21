using Application.Common;
using Application.Identity.Avatar;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Identity.UpdateAvatar;

public sealed class UpdateAvatarCommandHandler : IRequestHandler<UpdateAvatarCommand, AuthenticatedUserDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly IUserAvatarStore _avatars;
    private readonly ILearnerProfileRepository _profiles;
    private readonly TimeProvider _clock;

    public UpdateAvatarCommandHandler(
        IUserAccountStore accounts,
        IUserAvatarStore avatars,
        ILearnerProfileRepository profiles,
        TimeProvider clock)
    {
        _accounts = accounts;
        _avatars = avatars;
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<AuthenticatedUserDto> Handle(UpdateAvatarCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);
        var now = _clock.GetUtcNow();

        await _avatars.SaveAsync(
            account.Id,
            new UserAvatarContent(request.Data, request.ContentType, now),
            cancellationToken);
        account.SetCustomPicture(now);
        await _accounts.UpdateAsync(account, cancellationToken);

        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);
        return AuthenticatedUserDto.From(account, profile);
    }
}
