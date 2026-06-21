using Application.Grammar.Dtos;
using Application.Learning.Ports;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using MediatR;

namespace Application.Grammar.GetGrammarCatalog;

/// <summary>
/// Returns the grammar catalog for the learner's level - the 50 shared learning-spine topics for
/// that level (every skill teaches the same set). Each grammar lesson is generated lazily when its
/// topic is opened, so no per-lesson rows exist until then.
/// </summary>
public sealed class GetGrammarCatalogQueryHandler
    : IRequestHandler<GetGrammarCatalogQuery, IReadOnlyList<GrammarTopicSummaryDto>>
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

    public GetGrammarCatalogQueryHandler(
        IVocabularyTopicRepository topics, ILearnerProfileRepository profiles)
    {
        _topics = topics;
        _profiles = profiles;
    }

    public async Task<IReadOnlyList<GrammarTopicSummaryDto>> Handle(
        GetGrammarCatalogQuery request, CancellationToken cancellationToken)
    {
        // "All levels": the whole spine as one progressively harder list (easiest first).
        if (request.AllLevels)
        {
            var all = new List<GrammarTopicSummaryDto>();
            foreach (var lvl in AllLevelsOrdered)
            {
                var levelTopics = await _topics.GetByLevelAsync(lvl, cancellationToken);
                all.AddRange(levelTopics.Select(GrammarTopicSummaryDto.FromTopic));
            }

            return all;
        }

        // An explicit level lets the learner browse any level's grammar; otherwise use their own.
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

        return topics.Select(GrammarTopicSummaryDto.FromTopic).ToList();
    }
}
