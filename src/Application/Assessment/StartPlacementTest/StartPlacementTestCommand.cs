using Application.Assessment.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Assessment.StartPlacementTest;

/// <summary>Begins a new adaptive CEFR placement test for a learner.</summary>
public sealed record StartPlacementTestCommand(Guid LearnerId, bool IncludeSpeaking = true)
    : IRequest<StartPlacementTestResult>;

public sealed record StartPlacementTestResult(
    Guid SessionId,
    TestStage CurrentStage,
    PlacementItemDto? FirstItem);
