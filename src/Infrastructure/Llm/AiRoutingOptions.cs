using Application.Ai;

namespace Infrastructure.Llm;

/// <summary>
/// Chooses which model each AI feature runs on. The paid primary (SOL) and the self-hosted Hermes
/// gateway differ by roughly two orders of magnitude in marginal cost, and most of EnglishAI's LLM
/// traffic does not need the expensive one: the speaking tutor is capped at 96 output tokens and is
/// by far the highest-volume caller, while writing assessment and the learning assistant are
/// low-volume and quality-critical (docs/development-guide.md rule 10 - cheapest model that fits the job).
///
/// Routing is per <see cref="AiFeature"/> because that discriminator already flows to every
/// completion through <see cref="Infrastructure.Ai.AiAdmissionContext"/>, so no call site changes.
/// A feature that is absent from <see cref="PremiumFeatures"/> - including the anonymous
/// <see cref="AiFeature.Other"/> default used by background backfill - runs on the budget model.
/// </summary>
public sealed class AiRoutingOptions
{
    public const string SectionName = "AiRouting";

    /// <summary>
    /// Features that justify the paid primary model, as a comma-separated list of
    /// <see cref="AiFeature"/> names. Deliberately short: everything not listed here is a bulk,
    /// cached or low-stakes call. A single scalar rather than an array so it binds from one
    /// environment variable (an array would need AiRouting__PremiumFeatures__0, __1, …).
    /// Unknown names are ignored rather than crashing startup, so a typo degrades one feature to
    /// the budget model instead of taking the app down.
    /// </summary>
    public string PremiumFeatures { get; set; } =
        $"{nameof(AiFeature.WritingAssessment)},{nameof(AiFeature.Assistant)}";

    /// <summary>The parsed set. Unparseable entries are dropped.</summary>
    public IReadOnlySet<AiFeature> ResolvePremiumFeatures()
    {
        var features = new HashSet<AiFeature>();
        foreach (var name in (PremiumFeatures ?? string.Empty).Split(
                     ',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (Enum.TryParse<AiFeature>(name, ignoreCase: true, out var feature))
                features.Add(feature);
        }

        return features;
    }
}
