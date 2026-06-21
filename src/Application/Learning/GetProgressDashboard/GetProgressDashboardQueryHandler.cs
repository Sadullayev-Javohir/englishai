using Application.Analytics.GetStudyStats;
using Application.Gamification.GetGamificationStatus;
using Application.Learning.GetGrowth;
using Application.Learning.GetLearnerOverview;
using Application.Learning.GetProgressInsight;
using Application.Learning.GetRecommendations;
using Application.Levels.GetLevelMap;
using MediatR;

namespace Application.Learning.GetProgressDashboard;

public sealed class GetProgressDashboardQueryHandler
    : IRequestHandler<GetProgressDashboardQuery, ProgressDashboardDto>
{
    private readonly ISender _sender;

    public GetProgressDashboardQueryHandler(ISender sender)
    {
        _sender = sender;
    }

    public async Task<ProgressDashboardDto> Handle(
        GetProgressDashboardQuery request,
        CancellationToken cancellationToken)
    {
        var overview = await _sender.Send(
            new GetLearnerOverviewQuery(request.LearnerId), cancellationToken);

        var studyStats = await _sender.Send(
            new GetStudyStatsQuery(request.LearnerId, request.Today), cancellationToken);
        var insight = await _sender.Send(
            new GetProgressInsightQuery(request.LearnerId, request.Today), cancellationToken);
        var growth = await _sender.Send(
            new GetGrowthQuery(request.LearnerId), cancellationToken);
        var recommendations = await _sender.Send(
            new GetRecommendationsQuery(request.LearnerId), cancellationToken);
        var gamification = await _sender.Send(
            new GetGamificationStatusQuery(request.LearnerId), cancellationToken);
        var levelMap = await _sender.Send(
            new GetLevelMapQuery(request.LearnerId, overview.OverallLevel), cancellationToken);

        return new ProgressDashboardDto(
            studyStats,
            insight,
            overview,
            growth,
            recommendations,
            gamification,
            levelMap);
    }
}
