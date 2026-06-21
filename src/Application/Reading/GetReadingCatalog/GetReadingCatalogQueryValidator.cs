using FluentValidation;

namespace Application.Reading.GetReadingCatalog;

public sealed class GetReadingCatalogQueryValidator : AbstractValidator<GetReadingCatalogQuery>
{
    public GetReadingCatalogQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
