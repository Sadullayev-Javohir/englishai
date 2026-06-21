using MediatR;

namespace Application.Admin.DeleteRecentLog;

/// <summary>
/// A super-admin dismisses a single line from the server page's recent warning/error buffer. Returns
/// whether the entry was still present (it may have already scrolled out of the bounded buffer).
/// </summary>
public sealed record DeleteRecentLogCommand(Guid RequestingUserId, Guid LogId) : IRequest<bool>;
