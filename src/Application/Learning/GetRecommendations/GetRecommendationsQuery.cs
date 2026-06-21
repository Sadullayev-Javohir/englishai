using Application.Learning.Dtos;
using MediatR;

namespace Application.Learning.GetRecommendations;

/// <summary>Returns priority-ordered, Uzbek-resolved recommendations for a learner.</summary>
public sealed record GetRecommendationsQuery(Guid LearnerId)
    : IRequest<IReadOnlyList<RecommendationDto>>;
