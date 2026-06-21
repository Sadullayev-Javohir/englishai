using FluentValidation;

namespace Application.Books.GetBook;

public sealed class GetBookQueryValidator : AbstractValidator<GetBookQuery>
{
    public GetBookQueryValidator()
    {
        RuleFor(x => x.BookId).NotEmpty();
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
