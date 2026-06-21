using FluentValidation;

namespace Application.Notifications.TriggerDailyDispatch;

public sealed class TriggerDailyDispatchCommandValidator : AbstractValidator<TriggerDailyDispatchCommand>
{
    public TriggerDailyDispatchCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
