using MediatR;

namespace Application.Admin.ForceDueReviews;

/// <summary>
/// Operator/testing aid: forces every word the requesting user has saved to become due for SRS
/// review right now, so the review screen (which only shows *due* cards) can be exercised without
/// waiting out the 3/7/21-day schedule. Acts only on the caller's OWN vocabulary -
/// <see cref="RequestingUserId"/> comes from the session, never the body - and the handler
/// authorizes it as an admin (403 otherwise). Named <c>RequestingUserId</c> (not <c>LearnerId</c>)
/// so the learner-ownership pipeline leaves it to the explicit admin check. Returns how many words
/// were affected.
/// </summary>
public sealed record ForceDueReviewsCommand(Guid RequestingUserId) : IRequest<ForceDueReviewsResultDto>;

/// <summary>The outcome of a force-due run: total saved words, how many were newly made due, and
/// how many are due now (the resulting review-queue size).</summary>
public sealed record ForceDueReviewsResultDto(int TotalWords, int MadeDue, int DueNow, int SeededWords);
