using MediatR;

namespace Application.Notifications.DeleteAdminBroadcast;

/// <summary>
/// A super-admin removes a previously sent broadcast from the operator console history. Returns
/// whether the row still existed. The acting user comes from the session, never the request.
/// </summary>
public sealed record DeleteAdminBroadcastCommand(Guid RequestingUserId, Guid BroadcastId) : IRequest<bool>;
