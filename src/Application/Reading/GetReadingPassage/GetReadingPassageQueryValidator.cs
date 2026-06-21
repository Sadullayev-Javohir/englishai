using FluentValidation;

namespace Application.Reading.GetReadingPassage;

public sealed class GetReadingPassageQueryValidator : AbstractValidator<GetReadingPassageQuery>
{
    public GetReadingPassageQueryValidator()
    {
        RuleFor(x => x.TopicId).NotEmpty();
    }
}
