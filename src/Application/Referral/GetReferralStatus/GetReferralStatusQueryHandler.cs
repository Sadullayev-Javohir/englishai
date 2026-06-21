using Application.Referral.Dtos;
using Application.Referral.Ports;
using Domain.Referral;
using MediatR;

namespace Application.Referral.GetReferralStatus;

public sealed class GetReferralStatusQueryHandler : IRequestHandler<GetReferralStatusQuery, ReferralStatusDto>
{
    private readonly IReferralService _referrals;
    private readonly IReferralStore _store;

    public GetReferralStatusQueryHandler(IReferralService referrals, IReferralStore store)
    {
        _referrals = referrals;
        _store = store;
    }

    public async Task<ReferralStatusDto> Handle(GetReferralStatusQuery request, CancellationToken cancellationToken)
    {
        var account = await _referrals.GetOrCreateAccountAsync(request.LearnerId, cancellationToken);
        var sent = await _store.GetReferralsByReferrerAsync(request.LearnerId, cancellationToken);

        var qualified = sent.Count(r => r.Status == ReferralStatus.Qualified);

        return new ReferralStatusDto(
            Code: account.Code,
            InvitedCount: sent.Count,
            QualifiedCount: qualified,
            RewardCap: ReferralAccount.MaxRewardedReferrals,
            BonusTopicsUnlocked: account.BonusTopicAllowance,
            RemainingSpeakingCredits: account.RemainingSpeakingCredits,
            RemainingWritingCredits: account.RemainingWritingCredits,
            TopicsPerReferral: ReferralAccount.TopicsPerReferral,
            SpeakingCreditsPerReferral: ReferralAccount.SpeakingCreditsPerReferral,
            WritingCreditsPerReferral: ReferralAccount.WritingCreditsPerReferral);
    }
}
