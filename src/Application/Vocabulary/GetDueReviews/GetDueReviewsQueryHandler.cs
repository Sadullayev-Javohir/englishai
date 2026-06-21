using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.GetDueReviews;

public sealed class GetDueReviewsQueryHandler
    : IRequestHandler<GetDueReviewsQuery, IReadOnlyList<DueReviewDto>>
{
    /// <summary>Multiple-choice options shown for a <see cref="MiniTestType.ClozeChoice"/> review:
    /// the correct word plus up to this many distractors.</summary>
    private const int ClozeOptionCount = 4;

    private readonly IVocabularyRepository _vocabulary;
    private readonly TimeProvider _clock;

    public GetDueReviewsQueryHandler(IVocabularyRepository vocabulary, TimeProvider clock)
    {
        _vocabulary = vocabulary;
        _clock = clock;
    }

    public async Task<IReadOnlyList<DueReviewDto>> Handle(
        GetDueReviewsQuery request, CancellationToken cancellationToken)
    {
        var due = await _vocabulary.GetDueForLearnerAsync(
            request.LearnerId, _clock.GetUtcNow(), cancellationToken);

        if (due.Count == 0)
            return Array.Empty<DueReviewDto>();

        // Only fetch the learner's full word list (needed for cloze distractors) when at least one
        // due item actually needs it - most reviews don't, and this spares the extra query.
        var needsDistractors = due.Any(item => item.NextMiniTestType() == MiniTestType.ClozeChoice);
        var pool = needsDistractors
            ? await _vocabulary.GetByLearnerIdAsync(request.LearnerId, cancellationToken)
            : Array.Empty<VocabularyItem>();

        var random = new Random();

        return due
            .Select(item => item.NextMiniTestType() == MiniTestType.ClozeChoice
                ? DueReviewDto.FromDomain(item, BuildClozeOptions(item, pool, random))
                : DueReviewDto.FromDomain(item))
            .ToList();
    }

    /// <summary>
    /// Builds the shuffled option list for a cloze-choice review: the correct word plus up to
    /// <see cref="ClozeOptionCount"/> - 1 distractors drawn from the learner's own vocabulary
    /// (same part of speech preferred, so options stay plausible), shuffled together. Never
    /// invents distractor words (rule 11) - every option is a real word the learner has studied.
    /// </summary>
    private static IReadOnlyList<string> BuildClozeOptions(
        VocabularyItem item, IReadOnlyList<VocabularyItem> pool, Random random)
    {
        var distractorPool = pool
            .Where(w => w.Id != item.Id && !string.Equals(w.Word, item.Word, StringComparison.OrdinalIgnoreCase))
            .ToList();

        var sameCategory = distractorPool.Where(w => w.PartOfSpeech == item.PartOfSpeech).ToList();
        var preferred = sameCategory.Count >= ClozeOptionCount - 1 ? sameCategory : distractorPool;

        var distractors = preferred
            .Select(w => w.Word)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(_ => random.Next())
            .Take(ClozeOptionCount - 1)
            .ToList();

        var options = new List<string> { item.Word };
        options.AddRange(distractors);
        return options.OrderBy(_ => random.Next()).ToList();
    }
}
