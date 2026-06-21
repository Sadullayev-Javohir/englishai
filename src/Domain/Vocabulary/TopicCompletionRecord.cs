using Domain.Assessment;
using Domain.Common;
using Domain.Learning;

namespace Domain.Vocabulary;

/// <summary>
/// Tracks a learner's progress toward fully mastering a single <see cref="VocabularyTopic"/> across
/// all six lesson modules (PROJECT-SPEC K.5 - the spiral-curriculum "Topic Coverage" record). Each
/// core lesson module (Vocabulary, Grammar, Reading, Writing, Speaking, Listening) contributes a 0-100 score; the
/// topic counts as <i>mastered</i> only once every module has reached <see cref="MasteryThreshold"/>.
/// The topic's CEFR level is denormalized here because the same topic recurs at each level with rising
/// difficulty, so a record is scoped to one (learner, topic) pair - which is one level by construction.
/// </summary>
public sealed class TopicCompletionRecord
{
    /// <summary>Minimum per-module score required to count a module as passed.</summary>
    public const int MasteryThreshold = 75;

    /// <summary>Minimum Grammar score required to count the module as passed.</summary>
    public const int GrammarMasteryThreshold = 70;

    /// <summary>
    /// The six lesson modules every topic must pass before it is mastered, in canonical order
    /// (PROJECT-SPEC K.5 spiral path: vocabulary, grammar, reading, writing, speaking and listening).
    /// The order is authoritative: a module is gated (locked) until
    /// every earlier module in this list is passed, so the learner cannot skip ahead with too many
    /// mistakes still uncorrected. It matches the frontend SkillPath order.
    /// </summary>
    public static readonly IReadOnlyList<SkillType> RequiredModules = new[]
    {
        SkillType.Vocabulary,
        SkillType.Grammar,
        SkillType.Reading,
        SkillType.Writing,
        SkillType.Speaking,
        SkillType.Listening
    };

    private readonly List<TopicModuleScore> _moduleScores = new();

    // Parameterless ctor for EF Core materialization.
    private TopicCompletionRecord()
    {
    }

    private TopicCompletionRecord(
        Guid id, Guid learnerId, Guid vocabularyTopicId, CefrLevel level, DateTimeOffset now)
    {
        Id = id;
        LearnerId = learnerId;
        VocabularyTopicId = vocabularyTopicId;
        Level = level;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public Guid VocabularyTopicId { get; private set; }

    /// <summary>The topic's CEFR level, captured when the record is started (spiral curriculum).</summary>
    public CefrLevel Level { get; private set; }

    /// <summary>When every module first reached the mastery threshold; null while the topic is unmastered.</summary>
    public DateTimeOffset? MasteredAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<TopicModuleScore> ModuleScores => _moduleScores;

    public bool IsMastered => MasteredAt is not null;

    /// <summary>How many of the six lesson modules have reached the mastery threshold.</summary>
    public int PassedModuleCount => RequiredModules.Count(IsModulePassed);

    public static TopicCompletionRecord Start(
        Guid learnerId, Guid vocabularyTopicId, CefrLevel level, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (vocabularyTopicId == Guid.Empty)
            throw new DomainException("Vocabulary topic id must not be empty.");

        return new TopicCompletionRecord(Guid.NewGuid(), learnerId, vocabularyTopicId, level, now);
    }

    /// <summary>The learner's best score for a module, or 0 if the module has not been attempted.</summary>
    public int ScoreFor(SkillType module) =>
        _moduleScores.FirstOrDefault(m => m.Module == module)?.Score ?? 0;

    public bool IsModulePassed(SkillType module) => ScoreFor(module) >= ThresholdFor(module);

    public static int ThresholdFor(SkillType module) =>
        module == SkillType.Grammar ? GrammarMasteryThreshold : MasteryThreshold;

    /// <summary>The first unfinished module in the pedagogical order; navigation remains open.</summary>
    public SkillType? RecommendedNextModule => RequiredModules.FirstOrDefault(module => !IsModulePassed(module));

    /// <summary>
    /// Every module is selectable while the topic is open. The canonical order is retained by
    /// <see cref="RecommendedNextModule"/>, while passing and topic mastery still require the same
    /// score threshold across all six modules.
    /// </summary>
    public bool IsModuleUnlocked(SkillType module) => RequiredModules.Contains(module);

    /// <summary>
    /// Records a module result, keeping the learner's best score for that module. Re-evaluates
    /// mastery afterward and returns true only on the transition to mastered (so the caller can
    /// celebrate it once), false otherwise.
    /// </summary>
    public bool RecordModule(SkillType module, int score, DateTimeOffset now)
    {
        TopicModuleScore.Validate(score);

        var existing = _moduleScores.FirstOrDefault(m => m.Module == module);
        if (existing is null)
            _moduleScores.Add(new TopicModuleScore(module, score, now));
        else
            existing.RecordIfBetter(score, now);

        UpdatedAt = now;

        if (MasteredAt is null && RequiredModules.All(IsModulePassed))
        {
            MasteredAt = now;
            return true;
        }

        return false;
    }

    public void ResetModule(SkillType module, DateTimeOffset now)
    {
        var existing = _moduleScores.FirstOrDefault(score => score.Module == module);
        if (existing is not null)
            _moduleScores.Remove(existing);

        MasteredAt = null;
        UpdatedAt = now;
    }
}
