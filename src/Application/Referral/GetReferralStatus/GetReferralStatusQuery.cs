using Application.Referral.Dtos;
using MediatR;

namespace Application.Referral.GetReferralStatus;

/// <summary>
/// Reads a learner's referral standing for the Profile "Invite friends" section: their code,
/// how many friends joined/qualified, and the bonus earned. Creates the learner's referral
/// account (and code) on first read so sharing works immediately.
/// </summary>
public sealed record GetReferralStatusQuery(Guid LearnerId) : IRequest<ReferralStatusDto>;
