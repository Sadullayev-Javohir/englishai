using Application.Identity.Ports;

namespace Infrastructure.Identity;

/// <summary>
/// In-memory <see cref="IAccountEraser"/> for the no-database dev/test path. Removes the account
/// from the in-memory account store so the user can no longer sign back into it; the remaining
/// in-memory learner stores are process-local state that resets on restart. The durable EF
/// adapter (<see cref="EfAccountEraser"/>) performs the full wipe in production.
/// </summary>
public sealed class InMemoryAccountEraser : IAccountEraser
{
    private readonly IUserAccountStore _accounts;

    public InMemoryAccountEraser(IUserAccountStore accounts)
    {
        _accounts = accounts;
    }

    public async Task EraseAsync(Guid learnerId, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(learnerId, cancellationToken);
        if (account is not null)
            await _accounts.DeleteAsync(account, cancellationToken);
    }
}
