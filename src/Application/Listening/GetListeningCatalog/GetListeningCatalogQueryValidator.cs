using FluentValidation;

namespace Application.Listening.GetListeningCatalog;

public sealed class GetListeningCatalogQueryValidator : AbstractValidator<GetListeningCatalogQuery>
{
    public GetListeningCatalogQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
