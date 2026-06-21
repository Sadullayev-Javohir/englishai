using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// The end-of-scene "how did you do" result for a roleplay. Holds a 0-100 score for each
/// <see cref="RoleplayDimension"/>; the overall score is their average, and the band follows the same
/// <see cref="SpeakingScoreThresholds.GoodOverall"/> cutoff the pronunciation scoring uses so the UI
/// colour logic stays consistent. The learner-facing Uzbek summary and tip are chosen from templates
/// by <see cref="WeakestDimension"/> / <see cref="StrongestDimension"/> (docs/development-guide.md rule 11) - this
/// object carries only numbers, never generated prose.
/// </summary>
public sealed class RoleplayEvaluation
{
    public RoleplayEvaluation(
        double taskCompletion,
        double fluency,
        double grammar,
        double appropriateness)
    {
        TaskCompletion = Validate(taskCompletion, nameof(taskCompletion));
        Fluency = Validate(fluency, nameof(fluency));
        Grammar = Validate(grammar, nameof(grammar));
        Appropriateness = Validate(appropriateness, nameof(appropriateness));
    }

    public double TaskCompletion { get; }
    public double Fluency { get; }
    public double Grammar { get; }
    public double Appropriateness { get; }

    /// <summary>The average of the four dimension scores, rounded to one decimal place.</summary>
    public double OverallScore => Math.Round((TaskCompletion + Fluency + Grammar + Appropriateness) / 4.0, 1);

    public PronunciationBand Band =>
        OverallScore >= SpeakingScoreThresholds.GoodOverall
            ? PronunciationBand.Good
            : PronunciationBand.NeedsImprovement;

    /// <summary>The dimension with the lowest score - what the learner should work on next.</summary>
    public RoleplayDimension WeakestDimension => Ranked().Last().Dimension;

    /// <summary>The dimension with the highest score - the strength to praise.</summary>
    public RoleplayDimension StrongestDimension => Ranked().First().Dimension;

    public double ScoreFor(RoleplayDimension dimension) => dimension switch
    {
        RoleplayDimension.TaskCompletion => TaskCompletion,
        RoleplayDimension.Fluency => Fluency,
        RoleplayDimension.Grammar => Grammar,
        RoleplayDimension.Appropriateness => Appropriateness,
        _ => throw new DomainException($"Unknown roleplay dimension: {dimension}.")
    };

    // Highest score first. Ties are broken by the RoleplayDimension order so the choice of
    // strongest/weakest is deterministic (important for testable, repeatable feedback).
    private IReadOnlyList<(RoleplayDimension Dimension, double Score)> Ranked() =>
        new[]
            {
                (RoleplayDimension.TaskCompletion, TaskCompletion),
                (RoleplayDimension.Fluency, Fluency),
                (RoleplayDimension.Grammar, Grammar),
                (RoleplayDimension.Appropriateness, Appropriateness),
            }
            .OrderByDescending(x => x.Item2)
            .ThenBy(x => (int)x.Item1)
            .ToList();

    private static double Validate(double score, string name)
    {
        if (score is < 0 or > 100)
            throw new DomainException($"Roleplay {name} score must be between 0 and 100.");
        return score;
    }
}
