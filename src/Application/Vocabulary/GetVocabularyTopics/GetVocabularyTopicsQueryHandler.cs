using Application.Common;
using Application.Learning.Ports;
using Application.Subscription.Access;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using MediatR;

namespace Application.Vocabulary.GetVocabularyTopics;

public sealed class GetVocabularyTopicsQueryHandler
    : IRequestHandler<GetVocabularyTopicsQuery, IReadOnlyList<VocabularyTopicSummaryDto>>
{
    /// <summary>Level used for a learner who has not taken the placement test yet.</summary>
    private const CefrLevel DefaultLevel = CefrLevel.A2;

    /// <summary>Every CEFR level, easiest-first - the order of the "all levels" progression.</summary>
    private static readonly CefrLevel[] AllLevelsOrdered =
    {
        CefrLevel.A1, CefrLevel.A2, CefrLevel.B1, CefrLevel.B2, CefrLevel.C1, CefrLevel.C2,
    };

    private readonly IVocabularyTopicRepository _topics;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ITopicSpeakingProgressStore _speakingProgress;
    private readonly ITopicCompletionStore _completions;
    private readonly IComplimentaryAccess _complimentary;
    private readonly ITopicAccessPolicy _access;

    public GetVocabularyTopicsQueryHandler(
        IVocabularyTopicRepository topics,
        ILearnerProfileRepository profiles,
        ITopicSpeakingProgressStore speakingProgress,
        ITopicCompletionStore completions,
        IComplimentaryAccess complimentary,
        ITopicAccessPolicy access)
    {
        _topics = topics;
        _profiles = profiles;
        _speakingProgress = speakingProgress;
        _completions = completions;
        _complimentary = complimentary;
        _access = access;
    }

    public async Task<IReadOnlyList<VocabularyTopicSummaryDto>> Handle(
        GetVocabularyTopicsQuery request, CancellationToken cancellationToken)
    {
        var learnedIds = await _speakingProgress.GetLearnedTopicIdsAsync(request.LearnerId, cancellationToken);
        var learnedSet = (learnedIds ?? Array.Empty<Guid>()).ToHashSet();

        // Carry per-topic mastery so the catalog can gate topics the same way the level roadmap does
        // (K.5 - mastered topics plus the next three unmastered topics stay open).
        var completions = await _completions.GetByLearnerAsync(request.LearnerId, cancellationToken);
        var completionByTopic = completions.ToDictionary(c => c.VocabularyTopicId);
        var fullAccess = await _complimentary.HasFullAccessAsync(request.LearnerId, cancellationToken);

        // The sequential lock (K.5) is a per-level chain, so the "all levels" view gates each level's
        // topics independently - a fresh chain per band, not one global chain across every level.
        IReadOnlyList<VocabularyTopicSummaryDto> LockLevel(IReadOnlyList<VocabularyTopic> levelTopics) =>
            VocabularyTopicSummaryDto.ApplySequentialLock(
                levelTopics
                    .Select(t => VocabularyTopicSummaryDto.FromDomain(
                        t,
                        learnedSet.Contains(t.Id),
                        completionByTopic.GetValueOrDefault(t.Id)))
                    .ToList(),
                fullAccess);

        var rows = new List<VocabularyTopicSummaryDto>();
        if (request.AllLevels)
        {
            // "All levels": the whole spine as one progressively harder list (easiest first).
            foreach (var lvl in AllLevelsOrdered)
                rows.AddRange(LockLevel(await _topics.GetByLevelAsync(lvl, cancellationToken)));
        }
        else
        {
            CefrLevel level;
            if (request.Level is { } requested)
            {
                level = requested;
            }
            else
            {
                var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
                level = profile?.OverallLevel ?? DefaultLevel;
            }

            rows.AddRange(LockLevel(await _topics.GetByLevelAsync(level, cancellationToken)));
        }

        // Trial paywall (H.1): flag topics past the free allowance (global oldest-started set).
        var hasFullAccess = await _access.HasFullAccessAsync(request.LearnerId, cancellationToken);
        var startedTopicIds = completions
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.VocabularyTopicId)
            .ToList();
        return VocabularyTopicSummaryDto.ApplyTrialPaywall(rows, hasFullAccess, startedTopicIds);
    }
}
