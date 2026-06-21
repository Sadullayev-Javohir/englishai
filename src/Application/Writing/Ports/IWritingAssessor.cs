using Domain.Assessment;
using Domain.Writing;

namespace Application.Writing.Ports;

/// <summary>
/// Port for the AI writing assessor (PROJECT-SPEC G.3). An implementation evaluates the
/// submission across the four dimensions and returns a structured <see cref="WritingAssessment"/>
/// - per-dimension 1-5 scores plus located issue codes - never free-form Uzbek prose
/// (docs/development-guide.md rules 10, 11). Real LLM and deterministic Local adapters sit behind this port,
/// selected by configuration like the rest of the AI integrations.
/// </summary>
public interface IWritingAssessor
{
    Task<WritingAssessment> AssessAsync(
        WritingTask task,
        string text,
        CefrLevel assessmentLevel,
        CancellationToken cancellationToken);
}

/// <summary>
/// Authoritative assessor for the learner-facing Writing module. When a live provider is configured,
/// implementations must surface provider failures rather than silently returning an offline heuristic
/// that the result screen could present as an AI score. Placement may continue using the resilient
/// <see cref="IWritingAssessor"/> path because completing that multi-stage test takes priority.
/// </summary>
public interface ITopicWritingAssessor : IWritingAssessor
{
}
