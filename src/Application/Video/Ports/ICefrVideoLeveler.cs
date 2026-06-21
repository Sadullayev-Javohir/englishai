using Domain.Assessment;

namespace Application.Video.Ports;

/// <summary>
/// Estimates the CEFR level of a video from its transcript (PROJECT-SPEC B.3). The LLM
/// thinks in English and returns a structured level code (docs/development-guide.md rule 11); the budget
/// model and an output-token cap keep cost down (rule 10). A deterministic Local
/// stand-in is used when no LLM key is configured.
/// </summary>
public interface ICefrVideoLeveler
{
    Task<CefrLevel> EstimateLevelAsync(string transcript, CancellationToken cancellationToken = default);
}
