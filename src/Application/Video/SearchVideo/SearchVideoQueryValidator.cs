using FluentValidation;

namespace Application.Video.SearchVideo;

public sealed class SearchVideoQueryValidator : AbstractValidator<SearchVideoQuery>
{
    /// <summary>Caps the page size so a crafted request can't ask the source for a huge page.</summary>
    private const int MaxPageSize = 30;

    public SearchVideoQueryValidator()
    {
        RuleFor(x => x.Query).NotEmpty().MaximumLength(200);
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
    }
}
