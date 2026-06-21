using Domain.Gamification;
using FluentValidation;

namespace Application.Gamification.RedeemDiscount;

public sealed class RedeemDiscountCommandValidator : AbstractValidator<RedeemDiscountCommand>
{
    public RedeemDiscountCommandValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
        RuleFor(x => x.CoinsCost)
            .Must(coinsCost => DiscountCatalog.FindByCoinsCost(coinsCost) is not null)
            .WithMessage("Unknown discount tier.");
    }
}
