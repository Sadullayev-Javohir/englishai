using Application.Common;
using Application.Identity.Avatar;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Identity.DeleteAvatar;

public sealed class DeleteAvatarCommandHandler : IRequestHandler<DeleteAvatarCommand, AuthenticatedUserDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly IUserAvatarStore _avatars;
    private readonly ILearnerProfileRepository _profiles;

    public DeleteAvatarCommandHandler(
        IUserAccountStore accounts,
        IUserAvatarStore avatars,
        ILearnerProfileRepository profiles)
    {
        _accounts = accounts;
        _avatars = avatars;
        _profiles = profiles;
    }

    public async Task<AuthenticatedUserDto> Handle(DeleteAvatarCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        await _avatars.DeleteAsync(account.Id, cancellationToken);
        account.RestoreExternalPicture(request.FallbackPictureUrl);
        await _accounts.UpdateAsync(account, cancellationToken);

        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);
        return AuthenticatedUserDto.From(account, profile);
    }
}
