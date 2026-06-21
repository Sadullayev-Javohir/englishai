using Domain.Assessment;
using Domain.Common;
using Domain.Retention;

namespace Domain.Learning;

/// <summary>
/// Aggregate root for everything the platform knows about a learner's progress
/// (PROJECT-SPEC Faza 2): current CEFR level, per-skill baseline scores seeded from
/// the placement test, the running activity log that drives decay-weighted skill
/// scores, and the error heatmap. It also evaluates the G.4 level-up criteria and
/// produces structured recommendations. All Uzbek wording is resolved elsewhere.
/// </summary>
public sealed class LearnerProfile
{
    private readonly List<SkillSeed> _seeds = new();
    private readonly List<SkillActivity> _activities = new();
    private readonly List<ErrorObservation> _errors = new();

    // Parameterless ctor for EF Core materialization.
    private LearnerProfile()
    {
    }

    private LearnerProfile(Guid learnerId, CefrLevel overallLevel, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        OverallLevel = overallLevel;
        CreatedAt = now;
        UpdatedAt = now;
        LastActivityAt = now;
    }

    /// <summary>
    /// A low speaking score below this is treated as a frustration signal (PROJECT-SPEC I.1)
    /// when the learner has not practised anything since.
    /// </summary>
    public const int SpeakingFrustrationThreshold = 60;

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public CefrLevel OverallLevel { get; private set; }

    /// <summary>When the most recent confirmation mini-test was passed (PROJECT-SPEC G.4).</summary>
    public DateTimeOffset? ConfirmationTestPassedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }

    /// <summary>
    /// When the learner was last engaged (recorded practice, a confirmation test, or a
    /// fresh placement). Drives the win-back ladder and the no-activity churn signal
    /// (PROJECT-SPEC I.1/I.2).
    /// </summary>
    public DateTimeOffset LastActivityAt { get; private set; }

    /// <summary>
    /// The most recent win-back stage already notified, so the daily job only sends a
    /// message when the learner crosses into a new stage (PROJECT-SPEC I.2 - never spammy).
    /// </summary>
    public InactivityStage LastWinBackStage { get; private set; } = InactivityStage.Active;

    /// <summary>
    /// Why the learner is studying English, chosen once during onboarding (goal-based onboarding).
    /// Asked right after the level-choice flow (placement / "Start from A1") so it lives on the same
    /// aggregate as the rest of the learner's progress. Never null - defaults to
    /// <see cref="Domain.Common.LearningGoal.Unspecified"/> until the learner picks one (or skips).
    /// Tailors topic recommendations and powers the founder dashboard's per-goal segments.
    /// </summary>
    public LearningGoal LearningGoal { get; private set; } = LearningGoal.Unspecified;

    public IReadOnlyList<SkillSeed> Seeds => _seeds;
    public IReadOnlyList<SkillActivity> Activities => _activities;
    public IReadOnlyList<ErrorObservation> Errors => _errors;

    /// <summary>
    /// Creates a profile from a finalized placement result, seeding every skill's
    /// baseline score (PROJECT-SPEC G.1 → Faza 2 link).
    /// </summary>
    public static LearnerProfile CreateFromPlacement(
        Guid learnerId,
        PlacementResult result,
        DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        var profile = new LearnerProfile(learnerId, result.OverallLevel, now);
        profile.ApplySeeds(result);
        return profile;
    }

    /// <summary>
    /// Creates a profile for a learner who skipped the placement test and chose to start
    /// from a fixed CEFR level (onboarding "Start from A1" path). Every skill is seeded at
    /// that level's representative score; the learner can still level up via the G.4 flow.
    /// </summary>
    public static LearnerProfile CreateAtLevel(Guid learnerId, CefrLevel level, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        var profile = new LearnerProfile(learnerId, level, now);
        var seed = (double)level.ToScore();
        foreach (var skill in Enum.GetValues<SkillType>())
            profile.SetSeed(skill, seed);
        return profile;
    }

    /// <summary>
    /// Re-seeds the profile from a fresh placement (e.g. a refresh test). Keeps the
    /// activity log and error history; only baselines and overall level are updated.
    /// </summary>
    public void ApplyPlacement(PlacementResult result, DateTimeOffset now)
    {
        OverallLevel = result.OverallLevel;
        ApplySeeds(result);
        UpdatedAt = now;
        LastActivityAt = now;
    }

    private void ApplySeeds(PlacementResult result)
    {
        var stageScore = new Dictionary<TestStage, int>();
        foreach (var stage in result.StageResults.Values)
            stageScore[stage.Stage] = stage.Score;

        var overallSeed = (double)result.OverallLevel.ToScore();

        // Every placement stage seeds its corresponding skill independently. A skill
        // intentionally omitted by a caller (for example optional Speaking in an exit
        // test) falls back to the overall level rather than borrowing another skill.
        SetSeed(SkillType.Vocabulary, StageOrOverall(TestStage.Vocabulary));
        SetSeed(SkillType.Grammar, StageOrOverall(TestStage.Grammar));
        SetSeed(SkillType.Listening, StageOrOverall(TestStage.Listening));
        SetSeed(SkillType.Reading, StageOrOverall(TestStage.Reading));
        SetSeed(SkillType.Speaking, StageOrOverall(TestStage.Speaking));
        SetSeed(SkillType.Writing, StageOrOverall(TestStage.Writing));

        double StageOrOverall(TestStage stage) =>
            stageScore.TryGetValue(stage, out var s) ? s : overallSeed;
    }

    private void SetSeed(SkillType skill, double score)
    {
        var existing = _seeds.FirstOrDefault(s => s.Skill == skill);
        if (existing is null)
            _seeds.Add(new SkillSeed(skill, score));
        else
            existing.UpdateScore(score);
    }

    /// <summary>Records a scored practice/assessment outcome for a skill.</summary>
    public void RecordActivity(SkillType skill, int score, DateTimeOffset occurredAt)
    {
        _activities.Add(new SkillActivity(skill, score, occurredAt));
        UpdatedAt = occurredAt;
        if (occurredAt > LastActivityAt)
            LastActivityAt = occurredAt;
    }

    /// <summary>Records an observed error for the heatmap.</summary>
    public void RecordError(ErrorCategory category, SkillType skill, DateTimeOffset occurredAt)
    {
        _errors.Add(new ErrorObservation(category, skill, occurredAt));
        UpdatedAt = occurredAt;
    }

    public void RecordError(
        ErrorCategory category,
        SkillType skill,
        DateTimeOffset occurredAt,
        string source,
        Guid sourceId,
        string? prompt,
        string? learnerAnswer,
        string? expectedAnswer,
        string? explanation)
    {
        _errors.Add(new ErrorObservation(
            category, skill, occurredAt, source, sourceId, prompt,
            learnerAnswer, expectedAnswer, explanation));
        UpdatedAt = occurredAt;
    }

    /// <summary>Records the result of a confirmation mini-test (PROJECT-SPEC G.4).</summary>
    public void RecordConfirmationTest(bool passed, DateTimeOffset at)
    {
        if (passed)
            ConfirmationTestPassedAt = at;
        UpdatedAt = at;
        if (at > LastActivityAt)
            LastActivityAt = at;
    }

    /// <summary>
    /// Days since the learner was last engaged, as of <paramref name="now"/> (never negative).
    /// </summary>
    public int DaysSinceLastActivity(DateTimeOffset now)
    {
        var days = (int)Math.Floor((now - LastActivityAt).TotalDays);
        return days < 0 ? 0 : days;
    }

    /// <summary>
    /// True when the most recent activity is a below-threshold speaking score with nothing
    /// practised since and still inside the recent window - the I.1 frustration signal.
    /// </summary>
    public bool HasUnresolvedSpeakingFrustration(DateTimeOffset now)
    {
        var latest = _activities
            .OrderByDescending(a => a.OccurredAt)
            .FirstOrDefault();

        if (latest is null)
            return false;

        var withinWindow = (now - latest.OccurredAt).TotalDays <= SkillScoreCalculator.WindowDays;
        return withinWindow
            && latest.Skill == SkillType.Speaking
            && latest.Score < SpeakingFrustrationThreshold;
    }

    /// <summary>
    /// The distinct calendar days (UTC) on which the learner recorded activity - the basis
    /// for cohort retention math (PROJECT-SPEC I.4).
    /// </summary>
    public IReadOnlyCollection<DateOnly> ActiveDays() =>
        _activities
            .Select(a => DateOnly.FromDateTime(a.OccurredAt.UtcDateTime))
            .Distinct()
            .ToList();

    /// <summary>
    /// Records that a win-back message for <paramref name="stage"/> has been sent, so the
    /// daily job does not repeat it until the learner reaches a different stage.
    /// </summary>
    public void RecordWinBack(InactivityStage stage, DateTimeOffset at)
    {
        LastWinBackStage = stage;
        UpdatedAt = at;
    }

    /// <summary>
    /// Resets the win-back stage when the learner becomes active again, so a future lapse
    /// re-triggers the ladder from the start.
    /// </summary>
    public void ResetWinBack() => LastWinBackStage = InactivityStage.Active;

    /// <summary>
    /// Sets (or later changes) the learner's onboarding goal. Validated against the defined enum
    /// values so a forged numeric payload cannot store an unknown goal. Idempotent.
    /// </summary>
    public void SetLearningGoal(LearningGoal goal)
    {
        if (!Enum.IsDefined(goal))
            throw new DomainException("Unknown learning goal.");

        LearningGoal = goal;
    }

    /// <summary>Current decay-weighted score for every skill (PROJECT-SPEC G.4).</summary>
    public IReadOnlyList<SkillScore> SkillScores(DateTimeOffset now)
    {
        return Enum.GetValues<SkillType>()
            .Select(skill => SkillScoreCalculator.Compute(skill, _activities, SeedFor(skill), now))
            .ToList();
    }

    private double SeedFor(SkillType skill) =>
        _seeds.FirstOrDefault(s => s.Skill == skill)?.Score ?? OverallLevel.ToScore();

    /// <summary>
    /// Error counts per category over the recent window, so the heatmap reflects
    /// what the learner is struggling with now rather than long-resolved mistakes.
    /// </summary>
    public IReadOnlyDictionary<ErrorCategory, int> ErrorHeatmap(DateTimeOffset now)
    {
        var cutoff = now.AddDays(-SkillScoreCalculator.WindowDays);
        return _errors
            .Where(e => e.OccurredAt >= cutoff)
            .GroupBy(e => e.Category)
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>Evaluates the G.4 level-up criteria as of <paramref name="now"/>.</summary>
    public LevelTransitionStatus LevelStatus(DateTimeOffset now)
    {
        var confirmationValid =
            ConfirmationTestPassedAt is { } passedAt &&
            (now - passedAt).TotalDays <= SkillScoreCalculator.WindowDays;

        return LevelTransition.Evaluate(OverallLevel, SkillScores(now), confirmationValid);
    }

    /// <summary>
    /// Advances the CEFR level if the learner meets the G.4 criteria, consuming the
    /// confirmation test so the next level requires a fresh one. Returns whether the
    /// level changed.
    /// </summary>
    public bool TryAdvanceLevel(DateTimeOffset now)
    {
        if (!LevelStatus(now).IsEligible)
            return false;

        OverallLevel = OverallLevel.StepUp();
        ConfirmationTestPassedAt = null;
        UpdatedAt = now;
        return true;
    }

    /// <summary>Priority-ordered structured recommendations for what to do next.</summary>
    public IReadOnlyList<Recommendation> Recommendations(DateTimeOffset now) =>
        RecommendationEngine.Recommend(SkillScores(now), ErrorHeatmap(now), LevelStatus(now));

    /// <summary>
    /// Weekly growth series per skill over the last <paramref name="weeks"/> weeks.
    /// Each point is the decay-weighted score computed as of that week's end, using
    /// only activity recorded up to that moment - so the curve reflects real history.
    /// </summary>
    public IReadOnlyList<GrowthPoint> GrowthHistory(DateTimeOffset now, int weeks = 8)
    {
        if (weeks < 1)
            throw new DomainException("Growth history must span at least one week.");

        var points = new List<GrowthPoint>();
        var skills = Enum.GetValues<SkillType>();

        for (var w = weeks - 1; w >= 0; w--)
        {
            var weekEnding = now.AddDays(-7 * w);
            var activitiesToDate = _activities.Where(a => a.OccurredAt <= weekEnding).ToList();

            foreach (var skill in skills)
            {
                var score = SkillScoreCalculator.Compute(skill, activitiesToDate, SeedFor(skill), weekEnding);
                points.Add(new GrowthPoint(weekEnding, skill, score.Score));
            }
        }

        return points;
    }
}
