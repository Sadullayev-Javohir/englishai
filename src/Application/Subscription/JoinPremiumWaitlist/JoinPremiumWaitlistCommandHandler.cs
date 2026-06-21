using Application.Common;
using Application.Subscription.Ports;
using Domain.Subscription;
using MediatR;

namespace Application.Subscription.JoinPremiumWaitlist;

public sealed class JoinPremiumWaitlistCommandHandler
    : IRequestHandler<JoinPremiumWaitlistCommand, JoinPremiumWaitlistResult>
{
    private readonly IPremiumWaitlistRepository _waitlist;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;

    public JoinPremiumWaitlistCommandHandler(
        IPremiumWaitlistRepository waitlist,
        TimeProvider clock,
        ICurrentUserAccessor? currentUser = null)
    {
        _waitlist = waitlist;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<JoinPremiumWaitlistResult> Handle(
        JoinPremiumWaitlistCommand request, CancellationToken cancellationToken)
    {
        var entry = PremiumWaitlistEntry.Create(
            request.Contact,
            request.InterestedPlan,
            _currentUser?.LearnerId,
            request.Source,
            _clock.GetUtcNow());

        // Already on the list: report success without touching anything. Saying "you are already
        // registered" would turn a public endpoint into an enumeration oracle, and re-adding would
        // put the same person on the launch email twice.
        var existing = await _waitlist.GetByContactAsync(entry.Contact, cancellationToken);
        if (existing is null)
            await _waitlist.AddAsync(entry, cancellationToken);

        return new JoinPremiumWaitlistResult(Accepted: true);
    }
}
