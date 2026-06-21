using Application.Learning.Dtos;
using MediatR;

namespace Application.Learning.GetRecommendedTopics;

/// <summary>
/// Returns the learner's goal-tailored topic suggestions for the current level (goal-based
/// onboarding). <see cref="Count"/> bounds how many are returned for the home strip. LearnerId is
/// the account id; the ownership pipeline behavior ensures a caller can only read their own.
/// </summary>
public sealed record GetRecommendedTopicsQuery(Guid LearnerId, int Count = 6)
    : IRequest<RecommendedTopicsDto>;
