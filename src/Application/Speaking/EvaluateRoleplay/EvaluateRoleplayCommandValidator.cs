using FluentValidation;

namespace Application.Speaking.EvaluateRoleplay;

public sealed class EvaluateRoleplayCommandValidator : AbstractValidator<EvaluateRoleplayCommand>
{
    public EvaluateRoleplayCommandValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
