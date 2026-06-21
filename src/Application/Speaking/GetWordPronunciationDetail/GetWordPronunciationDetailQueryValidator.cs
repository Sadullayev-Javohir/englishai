using FluentValidation;

namespace Application.Speaking.GetWordPronunciationDetail;

public sealed class GetWordPronunciationDetailQueryValidator
    : AbstractValidator<GetWordPronunciationDetailQuery>
{
    public GetWordPronunciationDetailQueryValidator()
    {
        RuleFor(x => x.Word).NotEmpty();
    }
}
