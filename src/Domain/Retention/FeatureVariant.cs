using Domain.Common;

namespace Domain.Retention;

/// <summary>
/// One arm of a feature-flag experiment (PROJECT-SPEC I.3), e.g. the "control" (daily goal
/// = 3 tasks) vs a "variant" (= 5 tasks). The <see cref="Weight"/> sets its share of the
/// deterministic bucket split; the optional <see cref="Value"/> carries the variant's
/// payload (a structured string the consuming feature interprets - never Uzbek wording).
/// </summary>
public sealed class FeatureVariant
{
    private FeatureVariant()
    {
        Name = null!;
    }

    public FeatureVariant(string name, int weight, string? value = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new DomainException("Feature variant name must not be empty.");
        if (weight <= 0)
            throw new DomainException("Feature variant weight must be positive.");

        Id = Guid.NewGuid();
        Name = name;
        Weight = weight;
        Value = value;
    }

    public Guid Id { get; private set; }
    public string Name { get; private set; }
    public int Weight { get; private set; }
    public string? Value { get; private set; }
}
