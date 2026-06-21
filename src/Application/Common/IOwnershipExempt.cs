namespace Application.Common;

/// <summary>
/// Marks a request as intentionally cross-learner: an authenticated caller is allowed to
/// read another learner's data through it. Requests implementing this interface are skipped
/// by <see cref="LearnerOwnershipBehavior{TRequest,TResponse}"/> even though they carry a
/// <c>Guid LearnerId</c> that differs from the caller.
///
/// Use sparingly and only for read-only, public-safe projections (e.g. leaderboard progress
/// views that deliberately exclude private data such as errors, AI insight and coupons).
/// </summary>
public interface IOwnershipExempt;
