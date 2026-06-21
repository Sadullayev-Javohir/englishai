using System.Diagnostics.Metrics;
using System.Net;
using Infrastructure.Gamification;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure.Redis;

public enum RedisWorkload
{
    Critical,
    Cache
}

public interface IRedisConnectionProvider
{
    IConnectionMultiplexer Get(RedisWorkload workload);
    string Key(RedisWorkload workload, string category, string suffix);
}

public sealed class RedisConnectionProvider : IRedisConnectionProvider, IDisposable
{
    private static readonly Counter<long> ConnectionEvents =
        new Meter("EnglishAI.Observability").CreateCounter<long>("englishai.redis.connection.events");

    private readonly RedisOptions _options;
    private readonly ILogger<RedisConnectionProvider> _logger;
    private readonly Lazy<IConnectionMultiplexer> _critical;
    private readonly Lazy<IConnectionMultiplexer> _cache;

    public RedisConnectionProvider(RedisOptions options, ILogger<RedisConnectionProvider> logger)
    {
        _options = options;
        _logger = logger;
        _critical = new Lazy<IConnectionMultiplexer>(() => Connect(options.EffectiveCritical(), RedisWorkload.Critical));
        _cache = new Lazy<IConnectionMultiplexer>(() => Connect(options.EffectiveCache(), RedisWorkload.Cache));
    }

    public IConnectionMultiplexer Get(RedisWorkload workload) =>
        workload == RedisWorkload.Critical ? _critical.Value : _cache.Value;

    public string Key(RedisWorkload workload, string category, string suffix)
    {
        var environment = string.IsNullOrWhiteSpace(_options.Environment)
            ? "development"
            : _options.Environment.Trim().ToLowerInvariant();
        return $"englishai:{environment}:{workload.ToString().ToLowerInvariant()}:{category}:{suffix}";
    }

    private IConnectionMultiplexer Connect(RedisEndpointOptions endpoint, RedisWorkload workload)
    {
        if (!endpoint.IsConfigured)
            throw new InvalidOperationException($"Redis {workload} endpoint is not configured.");

        var options = ConfigurationOptions.Parse(
            string.IsNullOrWhiteSpace(endpoint.SentinelConnectionString)
                ? endpoint.ConnectionString
                : endpoint.SentinelConnectionString);
        if (!string.IsNullOrWhiteSpace(endpoint.SentinelConnectionString) && !string.IsNullOrWhiteSpace(endpoint.ConnectionString))
        {
            var data = ConfigurationOptions.Parse(endpoint.ConnectionString);
            options.User = data.User;
            options.Password = data.Password;
            options.Ssl = options.Ssl || data.Ssl;
        }
        options.AbortOnConnectFail = false;
        options.ConnectRetry = 5;
        options.ConnectTimeout = 3000;
        options.SyncTimeout = 3000;
        options.AsyncTimeout = 3000;
        options.KeepAlive = 30;

        IConnectionMultiplexer connection;
        if (!string.IsNullOrWhiteSpace(endpoint.ServiceName))
        {
            options.ServiceName = endpoint.ServiceName;
            connection = ConnectionMultiplexer.SentinelConnect(options).GetSentinelMasterConnection(options);
        }
        else
        {
            connection = ConnectionMultiplexer.Connect(options);
        }

        connection.ConnectionFailed += (_, args) => Record(workload, "failed", args.EndPoint);
        connection.ConnectionRestored += (_, args) => Record(workload, "restored", args.EndPoint);
        connection.ConfigurationChanged += (_, args) => Record(workload, "primary_changed", args.EndPoint);
        return connection;
    }

    private void Record(RedisWorkload workload, string eventName, EndPoint? endpoint)
    {
        ConnectionEvents.Add(1, new("workload", workload.ToString().ToLowerInvariant()), new("event", eventName));
        _logger.LogInformation("Redis {Workload} connection event {EventName} at {Endpoint}.", workload, eventName, endpoint);
    }

    public void Dispose()
    {
        if (_critical.IsValueCreated) _critical.Value.Dispose();
        if (_cache.IsValueCreated && !ReferenceEquals(_cache.Value, _critical.IsValueCreated ? _critical.Value : null))
            _cache.Value.Dispose();
    }
}
