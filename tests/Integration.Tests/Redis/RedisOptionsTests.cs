using FluentAssertions;
using Infrastructure.Gamification;
using Infrastructure.Redis;
using Microsoft.Extensions.Logging.Abstractions;

namespace Integration.Tests.Redis;

public sealed class RedisOptionsTests
{
    [Fact]
    public void Legacy_connection_configures_both_workloads()
    {
        var options = new RedisOptions { ConnectionString = "localhost:6379" };

        options.IsConfigured.Should().BeTrue();
        options.IsHighAvailability.Should().BeFalse();
        options.EffectiveCritical().ConnectionString.Should().Be("localhost:6379");
        options.EffectiveCache().ConnectionString.Should().Be("localhost:6379");
    }

    [Fact]
    public void Sentinel_configuration_requires_both_workloads_for_ha()
    {
        var options = new RedisOptions
        {
            Critical = new RedisEndpointOptions
            {
                SentinelConnectionString = "10.42.10.41:26379,10.42.10.42:26379",
                ConnectionString = "user=englishai-app,password=secret",
                ServiceName = "englishai-critical"
            },
            Cache = new RedisEndpointOptions
            {
                SentinelConnectionString = "10.42.10.41:26379,10.42.10.42:26379",
                ConnectionString = "user=englishai-app,password=secret",
                ServiceName = "englishai-cache"
            }
        };

        options.IsHighAvailability.Should().BeTrue();
    }

    [Fact]
    public void Namespace_separates_critical_and_cache_keys()
    {
        using var provider = new RedisConnectionProvider(
            new RedisOptions { Environment = "Production" },
            NullLogger<RedisConnectionProvider>.Instance);

        provider.Key(RedisWorkload.Critical, "placement", "abc")
            .Should().Be("englishai:production:critical:placement:abc");
        provider.Key(RedisWorkload.Cache, "ai", "abc")
            .Should().Be("englishai:production:cache:ai:abc");
    }
}
