using Application.Learning.Ports;
using Application.Vocabulary.Ports;
using Application.Writing.Dtos;
using Domain.Assessment;
using Domain.Vocabulary;
using MediatR;

namespace Application.Writing.GetWritingCatalog;

/// <summary>
/// Returns the writing catalog as the 50 shared learning-spine topics for a level (every skill
/// teaches the same set, so the catalog key is the topic id and each task is generated lazily when
/// its topic is opened). The view depends on the filters: an explicit
/// <see cref="GetWritingCatalogQuery.Level"/> browses that single band;
/// <see cref="GetWritingCatalogQuery.AllLevels"/> returns the whole A1→C2 ladder easiest-first;
/// otherwise it adapts to the learner's own level.
/// </summary>
public sealed class GetWritingCatalogQueryHandler
    : IRequestHandler<GetWritingCatalogQuery, IReadOnlyList<WritingTaskSummaryDto>>
{
    /// <summary>Starting level for a learner who has not taken the placement test yet.</summary>
    private const CefrLevel DefaultLevel = CefrLevel.A2;

    /// <summary>Every CEFR level, easiest-first - the order of the "all levels" progression.</summary>
    private static readonly CefrLevel[] AllLevelsOrdered =
    {
        CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2,
    };

    private readonly IVocabularyTopicRepository _topics;
    private readonly ILearnerProfileRepository _profiles;

    public GetWritingCatalogQueryHandler(
        IVocabularyTopicRepository topics, ILearnerProfileRepository profiles)
    {
        _topics = topics;
        _profiles = profiles;
    }

    public async Task<IReadOnlyList<WritingTaskSummaryDto>> Handle(
        GetWritingCatalogQuery request, CancellationToken cancellationToken)
    {
        // "All levels": the whole spine as one progressively harder list (easiest first).
        if (request.AllLevels)
        {
            var all = new List<VocabularyTopic>();
            foreach (var lvl in AllLevelsOrdered)
                all.AddRange(await _topics.GetByLevelAsync(lvl, cancellationToken));
            return all.Select(WritingTaskSummaryDto.FromTopic).ToList();
        }

        // An explicit level browses exactly that band; otherwise adapt to the learner's own level.
        CefrLevel level;
        if (request.Level.HasValue)
        {
            level = request.Level.Value;
        }
        else
        {
            var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
            level = profile?.OverallLevel ?? DefaultLevel;
        }

        var topics = await _topics.GetByLevelAsync(level, cancellationToken);

        return topics.Select(WritingTaskSummaryDto.FromTopic).ToList();
    }
}
