using Application.Gamification.Dtos;
using MediatR;

namespace Application.Gamification.GetEnergy;

public sealed record GetEnergyQuery(Guid LearnerId) : IRequest<EnergyDto>;
