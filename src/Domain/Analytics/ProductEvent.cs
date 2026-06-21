using Domain.Common;

namespace Domain.Analytics;

/// <summary>
/// Immutable product analytics fact for a learner action. These events power the founder activation
/// funnel (registered → placement → speaking → retention → monetization) without coupling the
/// dashboard to volatile UI state.
/// </summary>
public sealed class ProductEvent
{
    public const int MaxSourceLength = 120;

    // Parameterless ctor for EF Core materialization.
    private ProductEvent()
    {
    }

    private ProductEvent(Guid learnerId, ProductEventType type, DateTimeOffset occurredAt, string? source)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        Type = type;
        OccurredAt = occurredAt;
        Source = NormalizeSource(source);
    }

    public Guid Id { get; private set; }
    public Guid LearnerId { get; private set; }
    public ProductEventType Type { get; private set; }
    public DateTimeOffset OccurredAt { get; private set; }

    /// <summary>
    /// Optional bounded context key (session id, topic id/code, plan, etc.) used for idempotent writes
    /// and debugging. It must never contain secrets or free-form learner text.
    /// </summary>
    public string? Source { get; private set; }

    public static ProductEvent Record(
        Guid learnerId, ProductEventType type, DateTimeOffset occurredAt, string? source = null)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("A product event needs a learner.");
        if (!Enum.IsDefined(type))
            throw new DomainException("Unknown product event type.");

        return new ProductEvent(learnerId, type, occurredAt, source);
    }

    private static string? NormalizeSource(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
            return null;

        var trimmed = source.Trim();
        if (trimmed.Length > MaxSourceLength)
            throw new DomainException($"Product event source must be at most {MaxSourceLength} characters.");

        return trimmed;
    }
}
