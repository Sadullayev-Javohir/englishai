using FluentValidation;

namespace Application.Notifications.GetAdminBroadcasts;

public sealed class GetAdminBroadcastsQueryValidator : AbstractValidator<GetAdminBroadcastsQuery>
{
    public GetAdminBroadcastsQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
