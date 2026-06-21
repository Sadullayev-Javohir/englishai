using Application.Common;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Learning.Ports;
using Domain.Assessment;
using Domain.Identity;
using MediatR;

namespace Application.Identity.DeleteAccount;

/// <summary>
/// Erases the account and every trace of the learner. The re-typed confirmation email is
/// re-checked against the stored account here (defence in depth - the SPA also gates on it),
/// then the durable learner data is wiped via <see cref="IAccountEraser"/> and the cache-only
/// gamification (streak/daily counters) and leaderboard entry are cleared. The Web layer drops
/// the session cookie.
/// </summary>
public sealed class DeleteAccountCommandHandler : IRequestHandler<DeleteAccountCommand>
{
    private readonly IUserAccountStore _accounts;
    private readonly IAccountEraser _eraser;
    private readonly IGamificationStore _gamification;
    private readonly ILearnerProfileRepository _profiles;
    private readonly ILeaderboardStore _leaderboard;

    public DeleteAccountCommandHandler(
        IUserAccountStore accounts,
        IAccountEraser eraser,
        IGamificationStore gamification,
        ILearnerProfileRepository profiles,
        ILeaderboardStore leaderboard)
    {
        _accounts = accounts;
        _eraser = eraser;
        _gamification = gamification;
        _profiles = profiles;
        _leaderboard = leaderboard;
    }

    public async Task Handle(DeleteAccountCommand request, CancellationToken cancellationToken)
    {
        var account = await _accounts.GetByIdAsync(request.UserId, cancellationToken)
            ?? throw new NotFoundException(nameof(UserAccount), request.UserId);

        // The email the user re-typed must match their own - a guard against an accidental or
        // misdirected deletion even though the session already identifies the account.
        if (!string.Equals(
                account.Email.Trim(),
                request.ConfirmationEmail.Trim(),
                StringComparison.OrdinalIgnoreCase))
        {
            throw new ConflictException("Confirmation email does not match the account email.");
        }

        // The level lives on the profile, which the eraser deletes below - read it first so the
        // leaderboard cleanup knows which level's board to remove the learner from.
        var level = (await _profiles.GetByLearnerIdAsync(account.Id, cancellationToken))?.OverallLevel;

        await _eraser.EraseAsync(account.Id, cancellationToken);
        await _gamification.DeleteLearnerAsync(account.Id, cancellationToken);

        if (level is { } cefrLevel)
            await _leaderboard.RemoveAsync(account.Id, cefrLevel, cancellationToken);
    }
}
