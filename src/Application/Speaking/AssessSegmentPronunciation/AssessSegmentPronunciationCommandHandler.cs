using Application.Speaking.Dtos;
using Application.Speaking.Common;
using Application.Speaking.Ports;
using Domain.Speaking;
using MediatR;

namespace Application.Speaking.AssessSegmentPronunciation;

/// <summary>
/// Scores one shadowing attempt against its known reference line. Mirrors
/// <c>CheckWordPronunciationCommandHandler</c> but for a whole sentence rather than a single word,
/// and returns the richer <see cref="PronunciationResultDto"/> as-is (no per-word "correct" gate -
/// the shadowing UI shows the full breakdown, not a pass/fail).
/// </summary>
public sealed class AssessSegmentPronunciationCommandHandler
    : IRequestHandler<AssessSegmentPronunciationCommand, PronunciationResultDto>
{
    private readonly IPronunciationAssessor _assessor;

    public AssessSegmentPronunciationCommandHandler(IPronunciationAssessor assessor) => _assessor = assessor;

    public async Task<PronunciationResultDto> Handle(
        AssessSegmentPronunciationCommand request, CancellationToken cancellationToken)
    {
        // No audio captured (e.g. a near-silent slice the client sent anyway): a zero score rather
        // than fabricating one, mirroring the single-word handler's "not recognized" branch.
        if (request.AudioContent.Length == 0)
            return new PronunciationResultDto(
                0, 0, 0, 0, PronunciationBand.NeedsImprovement, IsAuthentic: false,
                Array.Empty<WordPronunciationDto>());

        var reference = EnglishSpokenForm.NormalizeText(request.ReferenceText.Trim());
        var result = await _assessor.AssessAsync(
            request.AudioContent, reference.Spoken, cancellationToken);

        return PronunciationResultDto.FromDomain(reference.MapResult(result));
    }
}
