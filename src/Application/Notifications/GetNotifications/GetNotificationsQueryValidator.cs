using FluentValidation;

namespace Application.Notifications.GetNotifications;

public sealed class GetNotificationsQueryValidator : AbstractValidator<GetNotificationsQuery>
{
    public GetNotificationsQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
