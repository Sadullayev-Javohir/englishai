using Application.Learning.Dtos;
using MediatR;

namespace Application.Learning.GetLearnerOverview;

/// <summary>Reads a learner's progress overview (level, skill scores, heatmap, level-up status).</summary>
public sealed record GetLearnerOverviewQuery(Guid LearnerId) : IRequest<LearnerOverviewDto>;
