using MediatR;

namespace Application.Identity.DeleteAccount;

/// <summary>
/// Permanently deletes the signed-in user's account and all their durable learner data.
/// <see cref="UserId"/> is taken from the validated session (never the request body) so a
/// caller can only ever delete their own account. <see cref="ConfirmationEmail"/> is the
/// email the user re-typed in the confirmation flow - a server-side safety gate that must
/// match the account's own email before anything is erased.
/// </summary>
public sealed record DeleteAccountCommand(Guid UserId, string ConfirmationEmail) : IRequest;
