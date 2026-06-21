using Application.Vocabulary.Dtos;
using Domain.Assessment;
using Domain.Learning;
using Domain.Levels;

namespace Application.Levels.Dtos;

/// <summary>
/// The Level Map for one CEFR level (PROJECT-SPEC M.3): the outer layer of the two-layer
/// navigation. Carries the level's can-do descriptors, its ordered topics (each topic opens the
/// inner 6-module path), the learner's progress through those topics, current skill scores and -
/// for the learner's current level - readiness for the Level Exit Test (M.5).
/// </summary>
public sealed record LevelMapDto(
    CefrLevel Level,
    CefrLevel CurrentLevel,
    bool IsCurrentLevel,
    bool IsLevelUnlocked,
    bool HasFullAccess,
    IReadOnlyList<LevelCanDoDto> CanDo,
    IReadOnlyList<VocabularyTopicSummaryDto> Topics,
    int TopicsTotal,
    int TopicsLearned,
    int TopicsMastered,
    Guid? ActiveTopicId,
    string? RecommendedNextModule,
    IReadOnlyList<LevelSkillScoreDto> SkillScores,
    LevelReadinessDto? Readiness,
    LevelExitState ExitState);

/// <summary>
/// State of a level's Exit Test node relative to the learner, so the roadmap can show a finish-line
/// gate at the end of <em>every</em> level (A1→C1), not only the current one (PROJECT-SPEC M.5):
/// <list type="bullet">
/// <item><see cref="Completed"/> - a level the learner has already passed (shown as done).</item>
/// <item><see cref="Current"/> - the learner's level; the test is actionable (see <c>Readiness</c>).</item>
/// <item><see cref="Locked"/> - a higher level not yet reached; the test is not yet available.</item>
/// <item><see cref="MaxLevel"/> - C2 has no next level, so there is no exit test (a trophy instead).</item>
/// </list>
/// </summary>
public enum LevelExitState
{
    Completed,
    Current,
    Locked,
    MaxLevel,
}

/// <summary>A single "can-do" statement (M.1). Uzbek text is resolved on the client from the
/// vetted template keyed by <paramref name="StatementCode"/> (rule 11).</summary>
public sealed record LevelCanDoDto(SkillType Skill, string StatementEn, string StatementCode)
{
    public static LevelCanDoDto FromDomain(CefrLevelDescriptor descriptor) =>
        new(descriptor.Skill, descriptor.StatementEn, descriptor.StatementCode);
}

/// <summary>A learner's current 0-100 score for one skill (PROJECT-SPEC G.4).</summary>
public sealed record LevelSkillScoreDto(SkillType Skill, double Score)
{
    public static LevelSkillScoreDto FromDomain(SkillScore score) => new(score.Skill, score.Score);
}

/// <summary>
/// Readiness for the Level Exit Test (PROJECT-SPEC M.5). <paramref name="ExitTestRecommended"/>
/// is true once the skill-mastery bar (G.4) is met, so the learner is invited to "prove it".
/// </summary>
public sealed record LevelReadinessDto(
    bool ExitTestRecommended,
    int MasteredSkillCount,
    int RequiredMasteredSkills,
    double MasteryThreshold,
    bool ProductiveSkillsMeetFloor,
    double ProductiveSkillFloor,
    bool HasSufficientSamples,
    int MinimumSamplesPerSkill,
    double MinimumOverallScore,
    double MinimumStageScore,
    double ProductiveStageFloor,
    bool AtMaxLevel)
{
    public static LevelReadinessDto FromDomain(
        CefrLevel currentLevel,
        LevelTransitionStatus status)
    {
        var requirements = currentLevel >= CefrLevelExtensions.Ceiling
            ? new LevelExitRequirements(
                currentLevel,
                currentLevel,
                100,
                100,
                100,
                LevelTransition.RequiredMasteredSkills,
                LevelTransition.MasteryThreshold,
                LevelTransition.ProductiveSkillFloor,
                LevelTransition.MinimumSamplesPerSkill)
            : LevelExitPolicy.RequirementsFor(currentLevel);
        return
        new(
            !status.AtMaxLevel &&
            status.MasteredSkillCount >= status.RequiredMasteredSkills &&
            status.ProductiveSkillsMeetFloor &&
            status.HasSufficientSamples,
            status.MasteredSkillCount,
            status.RequiredMasteredSkills,
            requirements.MasteryThreshold,
            status.ProductiveSkillsMeetFloor,
            requirements.ProductiveSkillFloor,
            status.HasSufficientSamples,
            requirements.MinimumSamplesPerSkill,
            requirements.MinimumOverallScore,
            requirements.MinimumStageScore,
            requirements.ProductiveStageFloor,
            status.AtMaxLevel);
    }
}
