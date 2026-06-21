using FluentValidation;

namespace Application.Referral.GetReferralStatus;

public sealed class GetReferralStatusQueryValidator : AbstractValidator<GetReferralStatusQuery>
{
    public GetReferralStatusQueryValidator()
    {
        RuleFor(x => x.LearnerId).NotEmpty();
    }
}
