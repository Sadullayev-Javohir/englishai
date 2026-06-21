using Application.Gamification.Dtos;
using Domain.Gamification;
using MediatR;

namespace Application.Gamification.ConsumeEnergy;

/// <summary>
/// Spends one energy on starting <paramref name="Action"/> for <paramref name="ReferenceId"/>.
/// Idempotent per learner+action+reference, so re-opening the same video or reconnecting to the
/// same conversation is free.
/// </summary>
/// <param name="ReferenceId">YouTube video id for Video, conversation session id for Speaking.</param>
public sealed record ConsumeEnergyCommand(
    Guid LearnerId,
    EnergyAction Action,
    string ReferenceId) : IRequest<EnergyDto>;
