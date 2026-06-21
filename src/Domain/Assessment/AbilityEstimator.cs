namespace Domain.Assessment;

/// <summary>
/// Estimates a learner's ability (on the 0-100 skill-score scale) from their
/// answers to multiple-choice items of known difficulty.
///
/// This replaces the old "highest difficulty answered correctly" rule, which
/// rewarded a single lucky guess: because every item is 4-option multiple choice,
/// a learner has a ~25% chance of guessing any question right, so one correct hard
/// answer used to inflate the whole placement (an A2 learner could be rated B2).
///
/// The model is a standard psychometric one - a logistic item-response curve with a
/// guessing floor (the "3PL" lower asymptote):
///
///     P(correct | ability a, difficulty d) = g + (1 - g) * sigmoid((a - d) / s)
///
/// where <c>g</c> is the guess probability (0.25 for 4 options) and <c>s</c> is the
/// logistic spread. We pick the ability that maximizes the likelihood of the
/// observed answers. Because a correct answer on a much harder item is already
/// "explained" by the guess floor, it barely moves the estimate - whereas a wrong
/// answer on an easy item pulls the estimate down strongly. That asymmetry is
/// exactly what stops lucky guesses from over-placing a learner.
///
/// On ties (a flat likelihood plateau, common with few items) we choose the lowest
/// ability within a small band, biasing toward a slightly conservative placement:
/// for a learning app, starting a touch too easy is far less harmful than the
/// frustration of being placed too hard.
/// </summary>
public static class AbilityEstimator
{
    /// <summary>Guess probability for a 4-option multiple-choice item.</summary>
    public const double GuessProbability = 0.25;

    /// <summary>Logistic spread on the 0-100 score scale (~ one CEFR band width).</summary>
    public const double Scale = 12.0;

    /// <summary>
    /// Width of the near-optimal log-likelihood band inside which we prefer the
    /// lowest ability (conservative placement on flat plateaus).
    /// </summary>
    public const double ConservativeBandLogLikelihood = 0.5;

    private const double MinAbility = 5.0;
    private const double MaxAbility = 100.0;
    private const double SearchStep = 1.0;
    private const double ProbabilityClamp = 1e-6;

    /// <summary>
    /// Returns the maximum-likelihood ability (0-100) for the given responses, or 0
    /// when there are none. Each response pairs an item's difficulty score with
    /// whether the learner answered it correctly.
    /// </summary>
    public static double Estimate(IReadOnlyCollection<AbilityResponse> responses)
    {
        if (responses.Count == 0)
            return 0;

        // First pass: find the peak log-likelihood over the ability grid.
        var peakLogLikelihood = double.NegativeInfinity;
        for (var ability = MinAbility; ability <= MaxAbility; ability += SearchStep)
        {
            var logLikelihood = LogLikelihood(ability, responses);
            if (logLikelihood > peakLogLikelihood)
                peakLogLikelihood = logLikelihood;
        }

        // Second pass: among abilities whose likelihood is within the conservative
        // band of the peak, return the lowest (walking low -> high, the first match).
        var threshold = peakLogLikelihood - ConservativeBandLogLikelihood;
        for (var ability = MinAbility; ability <= MaxAbility; ability += SearchStep)
        {
            if (LogLikelihood(ability, responses) >= threshold)
                return ability;
        }

        return MinAbility;
    }

    private static double LogLikelihood(double ability, IReadOnlyCollection<AbilityResponse> responses)
    {
        var total = 0.0;
        foreach (var response in responses)
        {
            var p = ProbabilityCorrect(ability, response.DifficultyScore);
            p = Math.Clamp(p, ProbabilityClamp, 1 - ProbabilityClamp);
            total += response.IsCorrect ? Math.Log(p) : Math.Log(1 - p);
        }

        return total;
    }

    /// <summary>P(correct) under the guessing-aware logistic model.</summary>
    public static double ProbabilityCorrect(double ability, double difficultyScore)
    {
        var sigmoid = 1.0 / (1.0 + Math.Exp(-(ability - difficultyScore) / Scale));
        return GuessProbability + (1 - GuessProbability) * sigmoid;
    }
}

/// <summary>A single graded item: its difficulty on the 0-100 scale and the outcome.</summary>
public readonly record struct AbilityResponse(double DifficultyScore, bool IsCorrect);
