using FluentValidation;

namespace Application.Admin.SetUserAdmin;

public sealed class SetUserAdminCommandValidator : AbstractValidator<SetUserAdminCommand>
{
    public SetUserAdminCommandValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.TargetUserId).NotEmpty();
    }
}
