using FluentValidation;

namespace Application.Identity.UpdateUserPreferences;

public sealed class UpdateUserPreferencesCommandValidator
    : AbstractValidator<UpdateUserPreferencesCommand>
{
    public UpdateUserPreferencesCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.DailyGoal).IsInEnum();
        RuleFor(x => x.LanguageBalance).IsInEnum();
    }
}
