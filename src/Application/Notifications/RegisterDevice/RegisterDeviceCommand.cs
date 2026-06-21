using MediatR;

namespace Application.Notifications.RegisterDevice;

/// <summary>
/// Registers (or refreshes) the calling learner's push token so the server can deliver native push to
/// this device. <see cref="LearnerId"/> is enforced against the JWT by the ownership pipeline, so a
/// learner can only register a token under their own account.
/// </summary>
public sealed record RegisterDeviceCommand(Guid LearnerId, string Token, string Platform) : IRequest<Unit>;
