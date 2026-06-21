namespace Application.Common;

/// <summary>
/// Grants a fixed allowlist of accounts full, unmetered access to the whole product - it
/// bypasses both the freemium gates (PROJECT-SPEC H.1: speaking/vocabulary/writing limits)
/// and the rolling topic-window locks (K.5: three unmastered topics stay open at a time).
/// Used to comp specific accounts (e.g. the founder/test account) without selling
/// them a subscription.
/// <para>
/// The allowlist is keyed on the account's <b>verified email</b>, resolved server-side from the
/// authenticated identity (the JWT subject → the persisted <see cref="Domain.Identity.UserAccount"/>).
/// A client never supplies the email, so the grant cannot be spoofed: only the real owner of an
/// allowlisted Google account is ever let in, and no amount of request tampering by another
/// learner can unlock anything (docs/development-guide.md §13).
/// </para>
/// </summary>
public interface IComplimentaryAccess
{
    /// <summary>
    /// Whether the learner's account is on the complimentary allowlist and therefore exempt from
    /// every lock. Returns false for unknown accounts and when the allowlist is empty.
    /// </summary>
    Task<bool> HasFullAccessAsync(Guid learnerId, CancellationToken cancellationToken);
}
