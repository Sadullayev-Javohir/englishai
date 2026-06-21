namespace Infrastructure.Gamification;

public sealed class RedisOptions
{
    public const string SectionName = "Redis";

    public string ConnectionString { get; set; } = string.Empty;
    public string Environment { get; set; } = "development";
    public RedisEndpointOptions Critical { get; set; } = new();
    public RedisEndpointOptions Cache { get; set; } = new();

    public bool IsConfigured => Critical.IsConfigured || Cache.IsConfigured || !string.IsNullOrWhiteSpace(ConnectionString);
    public bool IsHighAvailability => Critical.IsConfigured && Cache.IsConfigured;

    public RedisEndpointOptions EffectiveCritical() =>
        Critical.IsConfigured ? Critical.WithDataConnection() : RedisEndpointOptions.FromLegacy(ConnectionString);

    public RedisEndpointOptions EffectiveCache() =>
        Cache.IsConfigured ? Cache.WithDataConnection() : RedisEndpointOptions.FromLegacy(ConnectionString);
}

public sealed class RedisEndpointOptions
{
    public string ConnectionString { get; set; } = string.Empty;
    public string ServiceName { get; set; } = string.Empty;
    public string SentinelConnectionString { get; set; } = string.Empty;

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(ConnectionString) ||
        (!string.IsNullOrWhiteSpace(ServiceName) && !string.IsNullOrWhiteSpace(SentinelConnectionString));

    public static RedisEndpointOptions FromLegacy(string connectionString) => new() { ConnectionString = connectionString };

    public RedisEndpointOptions WithDataConnection()
    {
        if (string.IsNullOrWhiteSpace(ConnectionString) || string.IsNullOrWhiteSpace(SentinelConnectionString))
            return this;

        return new RedisEndpointOptions
        {
            ConnectionString = ConnectionString,
            ServiceName = ServiceName,
            SentinelConnectionString = SentinelConnectionString
        };
    }
}
