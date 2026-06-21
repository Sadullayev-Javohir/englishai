using MediatR;

namespace Application.Identity.ProcessTrialExpiry;

/// <summary>
/// Daily sweep that tells learners their free Pro trial is ending.
///
/// Without it the trial simply stops one morning and the learner discovers it by being refused -
/// the worst possible moment to ask someone for money.
/// </summary>
public sealed record ProcessTrialExpiryCommand : IRequest<ProcessTrialExpiryResult>;

public sealed record ProcessTrialExpiryResult(int RemindedLearners, int ExpiredTrials);
