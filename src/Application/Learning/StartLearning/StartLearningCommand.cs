using Domain.Assessment;
using MediatR;

namespace Application.Learning.StartLearning;

/// <summary>
/// Onboards a learner who skipped the placement test by creating their profile at a chosen
/// starting CEFR level (the "Start from A1" path). Idempotent: if a profile already exists
/// (e.g. the learner has since taken the placement test) it is left untouched.
/// </summary>
public sealed record StartLearningCommand(Guid LearnerId, CefrLevel Level) : IRequest<Unit>;
