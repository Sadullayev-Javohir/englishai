using Application.Listening.Dtos;
using MediatR;

namespace Application.Listening.GetListeningAudio;

/// <summary>
/// Returns the synthesized audio clip for a topic's listening exercise (keyed by the learning-spine
/// topic id). The clip is the same for every learner, so it is synthesized once (Azure Neural TTS)
/// and cached (docs/development-guide.md rule 10).
/// </summary>
public sealed record GetListeningAudioQuery(Guid TopicId) : IRequest<ListeningAudioResult>;
