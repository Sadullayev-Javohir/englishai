using Application.Learning.Dtos;
using MediatR;

namespace Application.Learning.GetProgressInsight;

public sealed record GetProgressInsightQuery(Guid LearnerId, DateOnly Today) : IRequest<ProgressInsightDto>;
