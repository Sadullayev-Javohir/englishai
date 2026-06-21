using Application.Learning.Dtos;
using MediatR;

namespace Application.Learning.RecordConfirmationTest;

/// <summary>
/// Records the outcome of a placement-style confirmation mini-test (PROJECT-SPEC
/// G.4) and, if all level-up criteria are now met, advances the learner's CEFR level.
/// </summary>
public sealed record RecordConfirmationTestCommand(Guid LearnerId, bool Passed)
    : IRequest<ConfirmationTestResultDto>;
