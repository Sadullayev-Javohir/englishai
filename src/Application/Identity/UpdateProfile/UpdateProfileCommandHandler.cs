using Application.Analytics.Ports;
using Application.Common;
using Application.Identity.Dtos;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Analytics;
using Domain.Identity;
using MediatR;

namespace Application.Identity.UpdateProfile;

public sealed class UpdateProfileCommandHandler
    : IRequestHandler<UpdateProfileCommand, AuthenticatedUserDto>
{
    private readonly IUserAccountStore _accounts;
    private readonly ILearnerProfileRepository _profiles;
    private readonly IProductEventStore _productEvents;
    private readonly TimeProvider _clock;

    public UpdateProfileCommandHandler(
        IUserAccountStore accounts,
        ILearnerProfileRepository profiles,
        IProductEventStore productEvents,
        TimeProvider clock)
    {
        _accounts = accounts;
        _profiles = profiles;
        _productEvents = productEvents;
        _clock = clock;
    }

    public async Task<AuthenticatedUserDto> Handle(
        UpdateProfileCommand request,
        CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        var normalized = UsernameRules.Normalize(request.Username);

        // Only pay for a uniqueness lookup when the handle actually changes; claiming one that
        // another account already holds is a 409 (distinct from a format 400).
        if (!string.Equals(normalized, account.Username, StringComparison.Ordinal))
        {
            var owner = await _accounts.GetByUsernameAsync(normalized, cancellationToken);
            if (owner is not null && owner.Id != account.Id)
                throw new ConflictException("Username is already taken.");
        }

        var firstUsernameSetup = account.Username is null;
        account.SetUsername(request.Username);
        account.UpdateDisplayName(request.DisplayName);
        await _accounts.UpdateAsync(account, cancellationToken);

        if (firstUsernameSetup)
            await _productEvents.AppendOnceAsync(
                account.Id,
                ProductEventType.UsernameSetupCompleted,
                _clock.GetUtcNow(),
                source: "profile",
                cancellationToken);

        var profile = await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken);
        return AuthenticatedUserDto.From(account, profile);
    }
}
