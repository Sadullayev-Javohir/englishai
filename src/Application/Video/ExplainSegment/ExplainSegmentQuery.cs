using Application.Video.Dtos;
using MediatR;

namespace Application.Video.ExplainSegment;

/// <summary>
/// Asks the explain-chat panel a free-text question about a video lesson. The handler resolves the
/// lesson's full real transcript server-side; <see cref="FocusText"/> identifies the current line for
/// word/sentence questions, while video-level questions use the whole transcript. <see cref="History"/>
/// is the panel's trailing client-side conversation context.
/// </summary>
public sealed record ExplainSegmentQuery(
    Guid VideoLessonId, string FocusText, string UserMessage, IReadOnlyList<ChatTurnDto> History)
    : IRequest<ChatReplyDto>;
