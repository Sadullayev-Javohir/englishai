using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using StackExchange.Redis;
using Infrastructure.Redis;
using Infrastructure.Storage;

namespace Infrastructure.Diagnostics;

public sealed class DatabaseSchemaHealthCheck(IServiceProvider services, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetService<Persistence.EnglishAiDbContext>();
        if (database is null)
            return string.IsNullOrWhiteSpace(configuration.GetConnectionString("Postgres"))
                ? HealthCheckResult.Healthy("PostgreSQL is not configured.")
                : HealthCheckResult.Unhealthy("PostgreSQL is configured but unavailable.");

        try
        {
            await Persistence.DatabaseInitializer.ValidateSchemaAsync(database, cancellationToken);
            return HealthCheckResult.Healthy("Database schema is current.");
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Database schema is not current.", exception);
        }
    }
}

public sealed class PostgreSqlHealthCheck(IServiceProvider services, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        await using var scope = services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetService<Persistence.EnglishAiDbContext>();
        if (database is null)
            return string.IsNullOrWhiteSpace(configuration.GetConnectionString("Postgres"))
                ? HealthCheckResult.Healthy("PostgreSQL is not configured.")
                : HealthCheckResult.Unhealthy("PostgreSQL is configured but unavailable.");

        return await database.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : HealthCheckResult.Unhealthy("PostgreSQL is unavailable.");
    }
}

public sealed class RedisHealthCheck(IServiceProvider services, IConfiguration configuration) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var redis = services.GetService<IRedisConnectionProvider>();
        if (redis is null)
            return string.IsNullOrWhiteSpace(configuration["Redis:ConnectionString"])
                   && string.IsNullOrWhiteSpace(configuration["Redis:Critical:ConnectionString"])
                   && string.IsNullOrWhiteSpace(configuration["Redis:Critical:SentinelConnectionString"])
                ? HealthCheckResult.Healthy("Redis is not configured.")
                : HealthCheckResult.Unhealthy("Redis is configured but unavailable.");

        try
        {
            await redis.Get(RedisWorkload.Critical).GetDatabase().PingAsync();
            await redis.Get(RedisWorkload.Cache).GetDatabase().PingAsync();
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Redis is unavailable.", exception);
        }
    }
}

public sealed class ObjectStorageHealthCheck(
    IServiceProvider services,
    ObjectStorageOptions options) : IHealthCheck
{
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!options.Enabled)
            return HealthCheckResult.Healthy("Object storage is disabled.");

        var client = services.GetService<IAmazonS3>();
        if (client is null)
            return HealthCheckResult.Unhealthy("Object storage is enabled but unavailable.");

        try
        {
            await ProbeBucketAsync(client, options.PublicBucket, cancellationToken);
            await ProbeBucketAsync(client, options.PrivateBucket, cancellationToken);
            return HealthCheckResult.Healthy();
        }
        catch (Exception exception)
        {
            return HealthCheckResult.Unhealthy("Object storage buckets are unavailable.", exception);
        }
    }

    private static async Task ProbeBucketAsync(IAmazonS3 client, string bucket, CancellationToken cancellationToken)
    {
        await client.ListObjectsV2Async(new ListObjectsV2Request
        {
            BucketName = bucket,
            MaxKeys = 1,
        }, cancellationToken);
    }
}
