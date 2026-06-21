namespace Application.Identity.Ports;

/// <summary>
/// Permanently erases all durable, learner-scoped data for a single account - the account row
/// itself plus every personal record keyed by the learner id (preferences, profile, vocabulary,
/// topic progress/completion, subscription, payments, book progress, study log). Shared catalog
/// and content (topics, passages, videos, lessons, books, images) is intentionally left intact.
/// Implemented by a durable EF Core adapter when a database is configured, and an in-memory
/// adapter otherwise. Cache-only stores (e.g. Redis gamification) are cleared separately.
/// </summary>
public interface IAccountEraser
{
    Task EraseAsync(Guid learnerId, CancellationToken cancellationToken);
}
