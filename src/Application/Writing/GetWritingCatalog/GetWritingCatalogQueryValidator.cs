using FluentValidation;

namespace Application.Writing.GetWritingCatalog;

public sealed class GetWritingCatalogQueryValidator : AbstractValidator<GetWritingCatalogQuery>
{
    public GetWritingCatalogQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
