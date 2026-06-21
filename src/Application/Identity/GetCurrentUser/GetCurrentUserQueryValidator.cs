using FluentValidation;

namespace Application.Identity.GetCurrentUser;

public sealed class GetCurrentUserQueryValidator : AbstractValidator<GetCurrentUserQuery>
{
    public GetCurrentUserQueryValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
    }
}
