namespace Application.Video.Ports;

public interface IVideoExplainCache
{
    Task<string?> GetAsync(string key, CancellationToken cancellationToken = default);
    Task SetAsync(string key, string reply, CancellationToken cancellationToken = default);
}
