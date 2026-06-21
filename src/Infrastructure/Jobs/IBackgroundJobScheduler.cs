namespace Infrastructure.Jobs;

public interface IBackgroundJobScheduler
{
    string EnqueueContent<TJob>(System.Linq.Expressions.Expression<Func<TJob, Task>> methodCall);
    string EnqueueMaintenance<TJob>(System.Linq.Expressions.Expression<Func<TJob, Task>> methodCall);
}
