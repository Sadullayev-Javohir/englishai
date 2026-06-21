using Hangfire;

namespace Infrastructure.Jobs;

public sealed class HangfireBackgroundJobScheduler(IBackgroundJobClient client) : IBackgroundJobScheduler
{
    public string EnqueueContent<TJob>(System.Linq.Expressions.Expression<Func<TJob, Task>> methodCall) =>
        client.Create(methodCall, new Hangfire.States.EnqueuedState(HangfireQueues.Content));

    public string EnqueueMaintenance<TJob>(System.Linq.Expressions.Expression<Func<TJob, Task>> methodCall) =>
        client.Create(methodCall, new Hangfire.States.EnqueuedState(HangfireQueues.Maintenance));
}
