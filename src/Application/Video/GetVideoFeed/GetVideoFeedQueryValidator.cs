using FluentValidation;

namespace Application.Video.GetVideoFeed;

public sealed class GetVideoFeedQueryValidator : AbstractValidator<GetVideoFeedQuery>
{
    /// <summary>Caps the page size so a crafted request can't ask the source for a huge page.</summary>
    private const int MaxPageSize = 30;

    public GetVideoFeedQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.PageSize).InclusiveBetween(1, MaxPageSize);
        RuleFor(x => x.VisitSeed).MaximumLength(100);
    }
}
