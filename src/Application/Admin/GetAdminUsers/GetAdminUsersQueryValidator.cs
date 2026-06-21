using FluentValidation;

namespace Application.Admin.GetAdminUsers;

public sealed class GetAdminUsersQueryValidator : AbstractValidator<GetAdminUsersQuery>
{
    public GetAdminUsersQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
        RuleFor(x => x.PageSize).InclusiveBetween(1, 100);
    }
}
