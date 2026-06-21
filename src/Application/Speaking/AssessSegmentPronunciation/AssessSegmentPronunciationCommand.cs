using Application.Speaking.Dtos;
using MediatR;

namespace Application.Speaking.AssessSegmentPronunciation;

/// <summary>
/// Scores a "shadowing" attempt - the learner speaking along with one video transcript
/// line/sentence - against that line's own known text (unlike <c>SubmitUtteranceCommand</c>, the
/// reference text doesn't need to be recognized via STT first, since the video transcript already
/// gives it). <see cref="AudioContent"/> is the raw clip the API binds from a base64 body field, a
/// slice the client cut from one continuous shadowing recording using the segment's own timestamps.
/// </summary>
public sealed record AssessSegmentPronunciationCommand(string ReferenceText, byte[] AudioContent)
    : IRequest<PronunciationResultDto>;
