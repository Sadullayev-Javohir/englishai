using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Application.Admin.Dtos;
using Application.Admin.Ports;

namespace Infrastructure.Diagnostics;

public sealed class InMemoryServerTelemetryStore : IServerTelemetryStore
{
    public const int DefaultSampleIntervalSeconds = 5;
    public const int DefaultRetentionMinutes = 60;

    private readonly object _gate = new();
    private readonly Queue<ServerTelemetryPointDto> _history = new();
    private readonly HashSet<Channel<ServerDiagnosticsDto>> _subscribers = new();
    private readonly int _capacity;
    private ServerDiagnosticsDto? _current;

    public InMemoryServerTelemetryStore(
        int sampleIntervalSeconds = DefaultSampleIntervalSeconds,
        int retentionMinutes = DefaultRetentionMinutes)
    {
        SampleIntervalSeconds = Math.Max(1, sampleIntervalSeconds);
        RetentionMinutes = Math.Max(1, retentionMinutes);
        _capacity = Math.Max(1, RetentionMinutes * 60 / SampleIntervalSeconds);
    }

    public int SampleIntervalSeconds { get; }
    public int RetentionMinutes { get; }
    public ServerDiagnosticsDto? Current
    {
        get { lock (_gate) return _current; }
    }

    public void Append(ServerDiagnosticsDto snapshot)
    {
        Channel<ServerDiagnosticsDto>[] subscribers;
        lock (_gate)
        {
            if (_current?.GeneratedAtUtc == snapshot.GeneratedAtUtc)
                return;

            _current = snapshot;
            _history.Enqueue(ToPoint(snapshot));
            while (_history.Count > _capacity)
                _history.Dequeue();
            subscribers = _subscribers.ToArray();
        }

        foreach (var subscriber in subscribers)
            subscriber.Writer.TryWrite(snapshot);
    }

    public IReadOnlyList<ServerTelemetryPointDto> History()
    {
        lock (_gate) return _history.ToArray();
    }

    public async IAsyncEnumerable<ServerDiagnosticsDto> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var channel = Channel.CreateBounded<ServerDiagnosticsDto>(new BoundedChannelOptions(4)
        {
            SingleReader = true,
            SingleWriter = false,
            FullMode = BoundedChannelFullMode.DropOldest,
        });

        lock (_gate) _subscribers.Add(channel);
        try
        {
            await foreach (var snapshot in channel.Reader.ReadAllAsync(cancellationToken))
                yield return snapshot;
        }
        finally
        {
            lock (_gate) _subscribers.Remove(channel);
            channel.Writer.TryComplete();
        }
    }

    private static ServerTelemetryPointDto ToPoint(ServerDiagnosticsDto snapshot) => new(
        snapshot.GeneratedAtUtc,
        snapshot.Runtime.ManagedMemoryMb,
        snapshot.Runtime.WorkingSetMb,
        snapshot.Runtime.ThreadCount,
        snapshot.Runtime.Gen0Collections,
        snapshot.Runtime.Gen1Collections,
        snapshot.Runtime.Gen2Collections,
        snapshot.Logs.WarningCount,
        snapshot.Logs.ErrorCount,
        snapshot.Dependencies.ToDictionary(item => item.Name, item => item.LatencyMs));
}
