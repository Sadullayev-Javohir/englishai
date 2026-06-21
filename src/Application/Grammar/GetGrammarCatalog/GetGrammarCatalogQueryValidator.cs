using FluentValidation;

namespace Application.Grammar.GetGrammarCatalog;

public sealed class GetGrammarCatalogQueryValidator : AbstractValidator<GetGrammarCatalogQuery>
{
    public GetGrammarCatalogQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
