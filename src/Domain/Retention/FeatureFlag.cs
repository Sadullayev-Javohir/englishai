using System.Security.Cryptography;
using System.Text;
using Domain.Common;

namespace Domain.Retention;

/// <summary>
/// A simple feature-flag experiment for the growth stage (PROJECT-SPEC I.3): a keyed flag
/// with one or more weighted variants. A learner is assigned a variant deterministically -
/// the same learner always lands in the same arm - by hashing the flag key with the learner
/// id, so groups are stable across requests without storing per-learner assignments. When
/// the flag is disabled every learner gets the first (control) variant.
/// </summary>
public sealed class FeatureFlag
{
    private readonly List<FeatureVariant> _variants = new();

    private FeatureFlag()
    {
        Key = null!;
        Description = null!;
    }

    public FeatureFlag(string key, string description, bool enabled, IEnumerable<FeatureVariant> variants)
    {
        if (string.IsNullOrWhiteSpace(key))
            throw new DomainException("Feature flag key must not be empty.");

        _variants = variants?.ToList() ?? new List<FeatureVariant>();
        if (_variants.Count == 0)
            throw new DomainException("A feature flag must define at least one variant.");

        Id = Guid.NewGuid();
        Key = key;
        Description = description ?? string.Empty;
        Enabled = enabled;
    }

    public Guid Id { get; private set; }
    public string Key { get; private set; }
    public string Description { get; private set; }
    public bool Enabled { get; private set; }
    public IReadOnlyList<FeatureVariant> Variants => _variants;

    /// <summary>
    /// The variant assigned to a learner. Disabled flags always resolve to the control
    /// (first) variant. Otherwise the learner is bucketed into <c>[0, totalWeight)</c> by a
    /// stable hash of <c>key:learnerId</c> and matched to the variant that owns that bucket.
    /// </summary>
    public FeatureVariant VariantFor(Guid learnerId)
    {
        var control = _variants[0];
        if (!Enabled)
            return control;

        var totalWeight = _variants.Sum(v => v.Weight);
        var bucket = StableBucket(learnerId, totalWeight);

        var cumulative = 0;
        foreach (var variant in _variants)
        {
            cumulative += variant.Weight;
            if (bucket < cumulative)
                return variant;
        }

        return control; // Unreachable: bucket < totalWeight always matches above.
    }

    private int StableBucket(Guid learnerId, int totalWeight)
    {
        var bytes = Encoding.UTF8.GetBytes($"{Key}:{learnerId:N}");
        var hash = SHA256.HashData(bytes);
        // First 4 bytes as an unsigned value → uniform bucket in [0, totalWeight).
        var value = BitConverter.ToUInt32(hash, 0);
        return (int)(value % (uint)totalWeight);
    }
}
