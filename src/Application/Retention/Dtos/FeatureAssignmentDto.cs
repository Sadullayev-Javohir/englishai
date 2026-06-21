using Domain.Retention;

namespace Application.Retention.Dtos;

/// <summary>
/// The variant a learner is assigned for a feature-flag experiment (PROJECT-SPEC I.3).
/// <see cref="Value"/> carries the variant's structured payload (e.g. the daily-goal size)
/// for the consuming feature to interpret.
/// </summary>
public sealed record FeatureAssignmentDto(string Key, string Variant, string? Value, bool Enabled)
{
    public static FeatureAssignmentDto From(FeatureFlag flag, Guid learnerId)
    {
        var variant = flag.VariantFor(learnerId);
        return new FeatureAssignmentDto(flag.Key, variant.Name, variant.Value, flag.Enabled);
    }
}
