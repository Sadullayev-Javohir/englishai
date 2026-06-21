using Application.Common;
using Application.Learning.Ports;
using Application.Levels.Dtos;
using Application.Subscription.Access;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Levels;
using MediatR;

namespace Application.Levels.GetLevelMap;

public sealed class GetLevelMapQueryHandler : IRequestHandler<GetLevelMapQuery, LevelMapDto>
{
    /// <summary>Level shown to a learner who has not taken the placement test yet.</summary>
    private const CefrLevel DefaultLevel = CefrLevel.A1;

    /// <summary>The highest CEFR level - it has no next level, so no Exit Test.</summary>
    private const CefrLevel MaxCefrLevel = CefrLevel.C2;

    private readonly IVocabularyTopicRepository _topics;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ITopicSpeakingProgressStore _speakingProgress;
    private readonly ITopicCompletionStore _completions;
    private readonly IComplimentaryAccess _complimentary;
    private readonly ITopicAccessPolicy _access;
    private readonly TimeProvider _clock;

    public GetLevelMapQueryHandler(
        IVocabularyTopicRepository topics,
        ILearnerProfileRepository profiles,
        ITopicSpeakingProgressStore speakingProgress,
        ITopicCompletionStore completions,
        IComplimentaryAccess complimentary,
        ITopicAccessPolicy access,
        TimeProvider clock)
    {
        _topics = topics;
        _profiles = profiles;
        _speakingProgress = speakingProgress;
        _completions = completions;
        _complimentary = complimentary;
        _access = access;
        _clock = clock;
    }

    public async Task<LevelMapDto> Handle(GetLevelMapQuery request, CancellationToken cancellationToken)
    {
        var profile = await _profiles.GetByLearnerIdAsync(request.LearnerId, cancellationToken);
        var currentLevel = profile?.OverallLevel ?? DefaultLevel;
        var level = request.Level ?? currentLevel;
        var isCurrentLevel = level == currentLevel;

        var topics = await _topics.GetByLevelAsync(level, cancellationToken);
        var learnedIds = await _speakingProgress.GetLearnedTopicIdsAsync(request.LearnerId, cancellationToken);
        var learnedSet = (learnedIds ?? Array.Empty<Guid>()).ToHashSet();

        // Per-topic six-module mastery progress (K.5) so the roadmap can render each topic's
        // completion ring without a request per topic.
        var completions = await _completions.GetByLearnerAsync(request.LearnerId, cancellationToken);
        var completionByTopic = completions.ToDictionary(c => c.VocabularyTopicId);

        // Topics arrive in the level's learning order (Sequence). The learner's current level keeps
        // a rolling three-unmastered-topic window; lower placement bands stay open for review.
        // Complimentary accounts bypass both gates and see every topic/module unlocked.
        var fullAccess = await _complimentary.HasFullAccessAsync(request.LearnerId, cancellationToken);
        var bypassProgressionGate = fullAccess || level < currentLevel;
        var topicDtos = VocabularyTopicSummaryDto.ApplySequentialLock(
            topics
                .Select(t => VocabularyTopicSummaryDto.FromDomain(
                    t,
                    learnedSet.Contains(t.Id),
                    completionByTopic.GetValueOrDefault(t.Id)))
                .ToList(),
            bypassProgressionGate);

        // Placement unlocks every lower CEFR band for review, keeps one frontier in the current band,
        // and locks every higher band. A server-verified complimentary account may open every level.
        var isLevelUnlocked = level <= currentLevel || fullAccess;
        if (!isLevelUnlocked)
        {
            topicDtos = topicDtos
                .Select(t => t with
                {
                    IsLocked = true,
                    RequiresPro = false,
                    Modules = t.Modules.Select(m => m with { Unlocked = false }).ToList(),
                })
                .ToList();
        }

        // Trial paywall (H.1): flag topics past the free allowance so the roadmap shows an upgrade
        // badge. The free set is global (oldest-started across all levels); premium/comped see no flag.
        var hasFullAccess = await _access.HasFullAccessAsync(request.LearnerId, cancellationToken);
        var startedTopicIds = completions
            .OrderBy(c => c.CreatedAt)
            .Select(c => c.VocabularyTopicId)
            .ToList();
        topicDtos = VocabularyTopicSummaryDto.ApplyTrialPaywall(topicDtos, hasFullAccess, startedTopicIds);

        var canDo = CefrLevelDescriptors.For(level)
            .Select(LevelCanDoDto.FromDomain)
            .ToList();

        // Skill scores and exit-test readiness only make sense once we have a profile, and the
        // readiness gate (G.4) is evaluated against the learner's *current* level, not a level
        // they are merely browsing.
        var now = _clock.GetUtcNow();
        var skillScores = profile is null
            ? new List<LevelSkillScoreDto>()
            : profile.SkillScores(now).Select(LevelSkillScoreDto.FromDomain).ToList();
        LevelReadinessDto? readiness = profile is not null && isCurrentLevel
            ? LevelReadinessDto.FromDomain(currentLevel, profile.LevelStatus(now))
            : null;

        // Every level A1→C1 ends with an Exit Test gate (M.5); C2 has no next level so it shows a
        // trophy instead. Past levels read as completed, higher levels as locked, so the finish line
        // appears at the end of each level while only the current one is actionable.
        var exitState = level == MaxCefrLevel
            ? LevelExitState.MaxLevel
            : level < currentLevel
                ? LevelExitState.Completed
                : level == currentLevel
                    ? LevelExitState.Current
                    : LevelExitState.Locked;

        var activeTopic = isLevelUnlocked
            ? topicDtos.FirstOrDefault(topic => !topic.IsMastered && !topic.IsLocked && !topic.RequiresPro)
            : null;
        var recommendedNextModule = activeTopic?.Modules
            .FirstOrDefault(module => module.Unlocked && !module.Passed)
            ?.Module;

        return new LevelMapDto(
            level,
            currentLevel,
            isCurrentLevel,
            isLevelUnlocked,
            fullAccess,
            canDo,
            topicDtos,
            topicDtos.Count,
            topicDtos.Count(t => t.Learned),
            topicDtos.Count(t => t.IsMastered),
            activeTopic?.Id,
            recommendedNextModule,
            skillScores,
            readiness,
            exitState);
    }
}
