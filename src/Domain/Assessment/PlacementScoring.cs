namespace Domain.Assessment;

/// <summary>
/// Pure scoring logic that turns the recorded answers of a placement session into
/// a <see cref="PlacementResult"/>. Separated from the session aggregate so the
/// scoring rules can be unit-tested in isolation.
/// </summary>
public static class PlacementScoring
{
    /// <summary>
    /// Relative weight of each independently measured skill when combining stage
    /// scores. Weights are renormalized only for callers that intentionally omit the
    /// optional Speaking stage (for example a level-exit test).
    /// </summary>
    public static readonly IReadOnlyDictionary<TestStage, double> StageWeights =
        new Dictionary<TestStage, double>
        {
            [TestStage.Vocabulary] = 1.0 / 6.0,
            [TestStage.Grammar] = 1.0 / 6.0,
            [TestStage.Listening] = 1.0 / 6.0,
            [TestStage.Reading] = 1.0 / 6.0,
            [TestStage.Writing] = 1.0 / 6.0,
            [TestStage.Speaking] = 1.0 / 6.0
        };

    public static PlacementResult Calculate(
        IReadOnlyCollection<AnswerRecord> answers,
        IReadOnlyCollection<ProductiveResult>? productiveResults = null)
    {
        var stageResults = new Dictionary<TestStage, StageResult>();

        // Multiple-choice stages: estimate ability from right/wrong answers.
        foreach (var group in answers.GroupBy(a => a.Stage))
        {
            var score = EstimateStageScore(group);
            var level = CefrLevelExtensions.FromScore(score);
            stageResults[group.Key] = new StageResult(group.Key, level, (int)Math.Round(score));
        }

        // Productive stages (Writing/Speaking): the task is already graded to 0-100.
        foreach (var productive in productiveResults ?? Array.Empty<ProductiveResult>())
        {
            var clamped = CapProductiveScore(productive.Score, productive.TaskDifficulty);
            stageResults[productive.Stage] = new StageResult(
                productive.Stage, CefrLevelExtensions.FromScore(clamped), clamped);
        }

        if (stageResults.Count == 0)
        {
            return new PlacementResult(CefrLevel.A1, 0, stageResults);
        }

        var totalWeight = stageResults.Keys.Sum(stage => StageWeights[stage]);
        var overallScore = stageResults.Values.Sum(
            result => result.Score * (StageWeights[result.Stage] / totalWeight));

        return new PlacementResult(
            CefrLevelExtensions.FromScore(overallScore),
            overallScore,
            stageResults);
    }

    public static int CapProductiveScore(int score, CefrLevel taskDifficulty)
    {
        var ceilingLevel = taskDifficulty.StepUp();
        var nextLevel = ceilingLevel.StepUp();
        var ceilingScore = nextLevel == ceilingLevel
            ? 100
            : (int)Math.Ceiling((ceilingLevel.ToScore() + nextLevel.ToScore()) / 2.0) - 1;

        return Math.Clamp(score, 0, ceilingScore);
    }

    /// <summary>
    /// A stage's score is the guessing-aware maximum-likelihood ability
    /// (<see cref="AbilityEstimator"/>) over the stage's answers, on the 0-100 scale.
    /// Unlike the old "highest difficulty answered correctly" rule, a single lucky
    /// guess on a hard item no longer inflates the result, and wrong answers count.
    /// </summary>
    private static double EstimateStageScore(IEnumerable<AnswerRecord> stageAnswers)
    {
        var responses = stageAnswers
            .Select(a => new AbilityResponse(a.Difficulty.ToScore(), a.IsCorrect))
            .ToList();

        return AbilityEstimator.Estimate(responses);
    }
}
