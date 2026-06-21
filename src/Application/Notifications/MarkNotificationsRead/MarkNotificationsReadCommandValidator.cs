using FluentValidation;

namespace Application.Notifications.MarkNotificationsRead;

public sealed class MarkNotificationsReadCommandValidator : AbstractValidator<MarkNotificationsReadCommand>
{
    public MarkNotificationsReadCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
