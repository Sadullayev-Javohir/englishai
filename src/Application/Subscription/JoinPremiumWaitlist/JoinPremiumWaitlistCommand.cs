using Domain.Subscription;
using MediatR;

namespace Application.Subscription.JoinPremiumWaitlist;

/// <summary>
/// Records that someone wants to be told when Premium can be bought. Anonymous: a visitor who has
/// not signed up yet is exactly the demand signal worth capturing.
/// </summary>
public sealed record JoinPremiumWaitlistCommand(
    string Contact,
    SubscriptionPlan? InterestedPlan = null,
    string? Source = null) : IRequest<JoinPremiumWaitlistResult>;

/// <param name="Accepted">
/// Always true for a well-formed contact, whether or not it was already on the list. The response
/// deliberately does not reveal which: a public endpoint that answers "this address is already
/// registered" is an account-enumeration oracle.
/// </param>
public sealed record JoinPremiumWaitlistResult(bool Accepted);
