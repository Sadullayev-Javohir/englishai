using Domain.Speaking;
using FluentValidation;

namespace Application.Speaking.StartRoleplay;

public sealed class StartRoleplayCommandValidator : AbstractValidator<StartRoleplayCommand>
{
    public StartRoleplayCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Level).IsInEnum();
        // Only a known scenario code from the curated catalog is accepted (resolved server-side).
        RuleFor(x => x.ScenarioCode)
            .NotEmpty()
            .Must(RoleplayScenarioCatalog.Contains)
            .WithMessage("Unknown roleplay scenario code.");
    }
}
