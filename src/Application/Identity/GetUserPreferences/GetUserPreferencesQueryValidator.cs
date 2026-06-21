using FluentValidation;

namespace Application.Identity.GetUserPreferences;

public sealed class GetUserPreferencesQueryValidator : AbstractValidator<GetUserPreferencesQuery>
{
    public GetUserPreferencesQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
