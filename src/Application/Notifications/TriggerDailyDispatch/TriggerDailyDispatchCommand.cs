using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Notifications.TriggerDailyDispatch;

/// <summary>
/// A super-admin manually runs the daily SRS reminder dispatch - the operator "send now" fallback for
/// when the scheduled Hangfire job has not run (or failed). The acting user comes from the session.
/// </summary>
public sealed record TriggerDailyDispatchCommand(Guid RequestingUserId) : IRequest<DispatchResultDto>;
