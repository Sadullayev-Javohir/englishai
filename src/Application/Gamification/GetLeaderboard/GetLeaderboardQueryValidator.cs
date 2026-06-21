using FluentValidation;

namespace Application.Gamification.GetLeaderboard;

public sealed class GetLeaderboardQueryValidator : AbstractValidator<GetLeaderboardQuery>
{
    public GetLeaderboardQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.Level!.Value).IsInEnum().When(x => x.Level.HasValue);
    }
}
