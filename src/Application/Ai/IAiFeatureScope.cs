namespace Application.Ai;

public interface IAiFeatureScope
{
    Task<IDisposable> EnterAsync(AiFeature feature, Guid? learnerId, CancellationToken cancellationToken);
}

public sealed class NoOpAiFeatureScope : IAiFeatureScope
{
    public static NoOpAiFeatureScope Instance { get; } = new();
    public Task<IDisposable> EnterAsync(AiFeature feature, Guid? learnerId, CancellationToken cancellationToken) =>
        Task.FromResult<IDisposable>(NoOpDisposable.Instance);

    private sealed class NoOpDisposable : IDisposable
    {
        public static NoOpDisposable Instance { get; } = new();
        public void Dispose() { }
    }
}
