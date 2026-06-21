using FluentValidation;

namespace Application.Books.GetBooksCatalog;

public sealed class GetBooksCatalogQueryValidator : AbstractValidator<GetBooksCatalogQuery>
{
    public GetBooksCatalogQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
