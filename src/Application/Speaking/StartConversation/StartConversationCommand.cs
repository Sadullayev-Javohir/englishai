using Application.Speaking.Dtos;
using Domain.Assessment;
using MediatR;

namespace Application.Speaking.StartConversation;

/// <summary>
/// Starts an AI speaking conversation pitched at the learner's CEFR level.
/// <para>
/// Topic selection (both optional; null/null means an open conversation):
/// <list type="bullet">
/// <item><paramref name="Topic"/> - a curated English code (e.g. "travel").</item>
/// <item><paramref name="VocabularyTopicId"/> - links the conversation to a vocabulary
/// topic the learner just studied. The handler resolves it server-side to that topic's
/// English title (the anchor) and its target words, which the tutor then encourages the
/// learner to use. Takes precedence over <paramref name="Topic"/>.</item>
/// </list>
/// </para>
/// </summary>
public sealed record StartConversationCommand(
    Guid LearnerId,
    CefrLevel Level,
    string? Topic = null,
    string? VocabularyTopicId = null)
    : IRequest<StartConversationResult>;

public sealed record StartConversationResult(
    Guid SessionId,
    string TutorText,
    string TutorAudioBase64,
    IReadOnlyList<VisemeFrameDto> Visemes,
    string? VisemeAnimation,
    TopicSpeakingProgressDto? TopicProgress = null,
    bool IsNaturalVoice = true,
    IReadOnlyList<SpeechWordTimingDto>? WordTimings = null);
