using FluentValidation;

namespace Application.Books.GetBookSection;

public sealed class GetBookSectionQueryValidator : AbstractValidator<GetBookSectionQuery>
{
    public GetBookSectionQueryValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.SectionId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
