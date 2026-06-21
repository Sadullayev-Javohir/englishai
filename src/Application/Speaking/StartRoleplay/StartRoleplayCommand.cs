using Application.Speaking.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Speaking.StartRoleplay;

/// <summary>
/// Starts a roleplay speaking sitting: the tutor greets the learner in character as the chosen
/// scenario's persona (interviewer, waiter, doctor, …) and the scene begins. The scenario is chosen
/// by its stable snake_case code from <see cref="Domain.Speaking.RoleplayScenarioCatalog"/> - the
/// client never sends persona text. Turns are then submitted through the ordinary
/// <c>/api/speaking/utterance</c> pipeline; the sitting is scored at the end via
/// <c>EvaluateRoleplayCommand</c>. Gated as a Speaking session (freemium, PROJECT-SPEC H.1).
/// </summary>
public sealed record StartRoleplayCommand(
    Guid LearnerId,
    CefrLevel Level,
    string ScenarioCode)
    : IRequest<StartRoleplayResult>;

public sealed record StartRoleplayResult(
    Guid SessionId,
    string ScenarioCode,
    string TutorText,
    string TutorAudioBase64,
    IReadOnlyList<VisemeFrameDto> Visemes,
    string? VisemeAnimation,
    bool IsNaturalVoice = true,
    IReadOnlyList<SpeechWordTimingDto>? WordTimings = null);
