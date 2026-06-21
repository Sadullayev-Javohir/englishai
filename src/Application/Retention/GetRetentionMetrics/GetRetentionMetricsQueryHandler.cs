using Application.Learning.Ports;
using Application.Retention.Dtos;
using Domain.Retention;
using MediatR;

namespace Application.Retention.GetRetentionMetrics;

/// <summary>
/// Aggregates the cohort's registration dates and active days into D1/D7/D30 retention
/// (PROJECT-SPEC I.4) via the pure <see cref="RetentionMetricsCalculator"/>.
/// </summary>
public sealed class GetRetentionMetricsQueryHandler
    : IRequestHandler<GetRetentionMetricsQuery, RetentionMetricsDto>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly TimeProvider _clock;

    public GetRetentionMetricsQueryHandler(ILearnerProfileRepository profiles, TimeProvider clock)
    {
        _profiles = profiles;
        _clock = clock;
    }

    public async Task<RetentionMetricsDto> Handle(
        GetRetentionMetricsQuery request, CancellationToken cancellationToken)
    {
        var asOf = DateOnly.FromDateTime(_clock.GetUtcNow().UtcDateTime);
        var profiles = await _profiles.GetAllAsync(cancellationToken);

        var windows = profiles
            .Select(p => new LearnerActivityWindow(
                DateOnly.FromDateTime(p.CreatedAt.UtcDateTime),
                p.ActiveDays()))
            .ToList();

        var metrics = RetentionMetricsCalculator.Compute(windows, asOf);
        return RetentionMetricsDto.From(metrics);
    }
}
