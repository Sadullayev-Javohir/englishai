using FluentValidation;

namespace Application.Notifications.DeleteAdminBroadcast;

public sealed class DeleteAdminBroadcastCommandValidator : AbstractValidator<DeleteAdminBroadcastCommand>
{
    public DeleteAdminBroadcastCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.BroadcastId).NotEmpty();
    }
}
