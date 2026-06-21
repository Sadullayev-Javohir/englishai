using Application.Common;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;

namespace Application.Vocabulary;

public sealed class MandatoryReviewPolicy : IMandatoryReviewPolicy
{
    private readonly IVocabularyRepository _vocabulary;
    private readonly TimeProvider _clock;

    public MandatoryReviewPolicy(IVocabularyRepository vocabulary, TimeProvider clock)
    {
        _vocabulary = vocabulary;
        _clock = clock;
    }

    public async Task<MandatoryReviewStatusDto> GetStatusAsync(
        Guid learnerId,
        CancellationToken cancellationToken)
    {
        var due = await _vocabulary.GetDueSummaryAsync(
            learnerId,
            _clock.GetUtcNow(),
            cancellationToken);

        return new MandatoryReviewStatusDto(
            due.ItemCount > 0,
            due.ItemCount,
            due.TopicCount);
    }

    public async Task EnsureAccessAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        var status = await GetStatusAsync(learnerId, cancellationToken);
        if (status.IsRequired)
            throw new MandatoryReviewRequiredException();
    }
}
