using Application.Learning.Dtos;
using MediatR;

namespace Application.Learning.GetGrowth;

/// <summary>Returns the weekly per-skill growth series for the progress charts.</summary>
public sealed record GetGrowthQuery(Guid LearnerId, int Weeks = 8)
    : IRequest<IReadOnlyList<GrowthPointDto>>;
