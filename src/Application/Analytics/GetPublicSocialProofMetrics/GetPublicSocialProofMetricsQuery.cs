using Application.Analytics.Dtos;
using MediatR;

namespace Application.Analytics.GetPublicSocialProofMetrics;

public sealed record GetPublicSocialProofMetricsQuery : IRequest<PublicSocialProofMetricsDto>;
