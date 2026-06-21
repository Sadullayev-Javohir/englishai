using FluentValidation;

namespace Application.Reading.Admin.GetAllReadingPassages;

public sealed class GetAllReadingPassagesQueryValidator : AbstractValidator<GetAllReadingPassagesQuery>
{
    public GetAllReadingPassagesQueryValidator()
    {
        RuleFor(x => x.RequestingUserId).NotEmpty();
    }
}
