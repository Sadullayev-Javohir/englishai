using Domain.Learning;
using Domain.Vocabulary;

namespace Application.Vocabulary.Dtos;

/// <summary>
/// A learner's progress toward mastering a topic across all six required lesson modules.
/// module score names are the canonical <see cref="SkillType"/> enum names; Uzbek labels are
/// resolved on the frontend from the content layer (rule 11).
/// </summary>
public sealed record TopicCompletionDto(
    Guid LearnerId,
    Guid TopicId,
    string Level,
    bool IsMastered,
    DateTimeOffset? MasteredAt,
    int MasteryThreshold,
    int PassedModuleCount,
    int RequiredModuleCount,
    IReadOnlyList<TopicModuleScoreDto> Modules)
{
    public static TopicCompletionDto FromDomain(TopicCompletionRecord record)
    {
        var scoreByModule = record.ModuleScores.ToDictionary(m => m.Module);

        var visibleModules = TopicCompletionRecord.RequiredModules
            .Append(SkillType.Grammar)
            .Distinct();

        var modules = visibleModules
            .Select(module =>
            {
                scoreByModule.TryGetValue(module, out var score);
                return new TopicModuleScoreDto(
                    module.ToString(),
                    score?.Score ?? 0,
                    record.IsModulePassed(module),
                    record.IsModuleUnlocked(module),
                    score?.AchievedAt);
            })
            .ToList();

        return new TopicCompletionDto(
            record.LearnerId,
            record.VocabularyTopicId,
            record.Level.ToString(),
            record.IsMastered,
            record.MasteredAt,
            TopicCompletionRecord.MasteryThreshold,
            record.PassedModuleCount,
            TopicCompletionRecord.RequiredModules.Count,
            modules);
    }

    /// <summary>
    /// Returns this progress with every module's sequential gate cleared (K.5 bypassed), for comped
    /// accounts (<see cref="Application.Common.IComplimentaryAccess"/>). Scores/passed flags are kept
    /// as they are - only the <c>Unlocked</c> gate is forced open so the learner can enter any module.
    /// </summary>
    public TopicCompletionDto AllModulesUnlocked() => this with
    {
        Modules = Modules.Select(m => m with { Unlocked = true }).ToList(),
    };
}

/// <summary>
/// One module's best score within a <see cref="TopicCompletionDto"/>. <paramref name="Unlocked"/>
/// is the sequential gate (K.5): false while an earlier module in the learning path still has too
/// many mistakes (is unpassed), so the learner cannot jump ahead to it.
/// </summary>
public sealed record TopicModuleScoreDto(string Module, int Score, bool Passed, bool Unlocked, DateTimeOffset? AchievedAt);
