using MediatR;

namespace Application.Notifications.MarkNotificationsRead;

/// <summary>Marks all of a learner's notifications as read ("Hammasini o'qish").</summary>
public sealed record MarkNotificationsReadCommand(Guid LearnerId) : IRequest;
