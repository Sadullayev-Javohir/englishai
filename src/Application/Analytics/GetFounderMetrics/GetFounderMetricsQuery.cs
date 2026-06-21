using Application.Analytics.Dtos;
using MediatR;

namespace Application.Analytics.GetFounderMetrics;

/// <summary>
/// The founder growth dashboard read (PROJECT-SPEC Faza 7). <see cref="RequestingUserId"/> is the
/// viewer from the session; the handler authorizes it as an admin and rejects everyone else with a
/// 403. Named <c>RequestingUserId</c> (not <c>LearnerId</c>) so the ownership pipeline behavior
/// leaves it alone - this is an operator-wide query, not learner-scoped data.
/// </summary>
public sealed record GetFounderMetricsQuery(
    Guid RequestingUserId,
    DateOnly? From = null,
    DateOnly? To = null) : IRequest<FounderMetricsDto>;
