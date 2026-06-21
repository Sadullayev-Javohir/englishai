using MediatR;

namespace Application.Admin.ClearRecentLogs;

/// <summary>
/// A super-admin empties the server page's recent warning/error buffer in one action. Returns how
/// many entries were removed.
/// </summary>
public sealed record ClearRecentLogsCommand(Guid RequestingUserId) : IRequest<int>;
