using Application.Identity.Dtos;
using Application.Identity.Ports;
using Domain.Identity;
using MediatR;

namespace Application.Identity.CheckUsernameAvailability;

public sealed class CheckUsernameAvailabilityQueryHandler
    : IRequestHandler<CheckUsernameAvailabilityQuery, UsernameAvailabilityDto>
{
    private readonly IUserAccountStore _accounts;

    public CheckUsernameAvailabilityQueryHandler(IUserAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task<UsernameAvailabilityDto> Handle(
        CheckUsernameAvailabilityQuery request,
        CancellationToken cancellationToken)
    {
        // Report format first so the SPA never claims an ill-formed handle is "available".
        if (!UsernameRules.IsValid(request.Username))
            return new UsernameAvailabilityDto(IsValidFormat: false, IsAvailable: false);

        var normalized = UsernameRules.Normalize(request.Username);
        var owner = await _accounts.GetByUsernameAsync(normalized, cancellationToken);

        // Free when nobody holds it, or the only holder is the requester themselves.
        var available = owner is null || owner.Id == request.ExcludingUserId;
        return new UsernameAvailabilityDto(IsValidFormat: true, IsAvailable: available);
    }
}
