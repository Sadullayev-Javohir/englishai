using FluentValidation;

namespace Application.Reading.Admin.GetReadingPassageAdmin;

public sealed class GetReadingPassageAdminQueryValidator : AbstractValidator<GetReadingPassageAdminQuery>
{
    public GetReadingPassageAdminQueryValidator()
    {
        RuleFor(query => query.RequestingUserId).NotEmpty();
        RuleFor(query => query.Id).NotEmpty();
    }
}
