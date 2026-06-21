using FluentValidation;

namespace Application.Speaking.GetIdeaCards;

public sealed class GetIdeaCardsQueryValidator : AbstractValidator<GetIdeaCardsQuery>
{
    public GetIdeaCardsQueryValidator()
    {
        RuleFor(x => x.SessionId).NotEmpty();
    }
}
