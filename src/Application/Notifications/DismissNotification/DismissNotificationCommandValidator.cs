using FluentValidation;

namespace Application.Notifications.DismissNotification;

public sealed class DismissNotificationCommandValidator : AbstractValidator<DismissNotificationCommand>
{
    public DismissNotificationCommandValidator()
    {
        RuleFor(c => c.LearnerId).NotEmpty();
        RuleFor(c => c.NotificationId).NotEmpty();
    }
}
