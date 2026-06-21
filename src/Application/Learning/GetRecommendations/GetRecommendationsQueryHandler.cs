using Application.Common;
using Application.Learning.Dtos;
using Application.Learning.Ports;
using Domain.Learning;
using MediatR;

namespace Application.Learning.GetRecommendations;

public sealed class GetRecommendationsQueryHandler
    : IRequestHandler<GetRecommendationsQuery, IReadOnlyList<RecommendationDto>>
{
    private readonly ILearnerProfileRepository _profiles;
    private readonly IRecommendationTemplateProvider _templates;
    private readonly TimeProvider _clock;

    public GetRecommendationsQueryHandler(
        ILearnerProfileRepository profiles,
        IRecommendationTemplateProvider templates,
        TimeProvider clock)
    {
        _profiles = profiles;
        _templates = templates;
        _clock = clock;
    }

    public async Task<IReadOnlyList<RecommendationDto>> Handle(
        GetRecommendationsQuery request,
        CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
            ?? throw new NotFoundException(nameof(LearnerProfile), request.LearnerId);

        return profile.Recommendations(_clock.GetUtcNow())
            .Select(r => new RecommendationDto(r.Code, _templates.Resolve(r), r.Skill, r.Category))
            .ToList();
    }
}
