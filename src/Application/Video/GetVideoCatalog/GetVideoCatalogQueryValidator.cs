using FluentValidation;

namespace Application.Video.GetVideoCatalog;

public sealed class GetVideoCatalogQueryValidator : AbstractValidator<GetVideoCatalogQuery>
{
    public GetVideoCatalogQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
