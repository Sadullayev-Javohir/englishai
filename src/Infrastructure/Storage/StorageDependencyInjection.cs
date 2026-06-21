using Amazon;
using Amazon.Runtime;
using Amazon.S3;
using Application.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Storage;

public static class StorageDependencyInjection
{
    public static IServiceCollection AddObjectStorage(this IServiceCollection services, IConfiguration configuration)
    {
        var options = configuration.GetSection(ObjectStorageOptions.SectionName).Get<ObjectStorageOptions>()
                      ?? new ObjectStorageOptions();
        var production = configuration["ASPNETCORE_ENVIRONMENT"]?.Equals("Production", StringComparison.OrdinalIgnoreCase) == true;
        options.Validate(production);
        services.AddSingleton(options);

        if (!options.Enabled)
        {
            services.AddSingleton<IObjectStorage, LocalObjectStorage>();
            return services;
        }

        services.AddSingleton<IAmazonS3>(_ => new AmazonS3Client(
            new BasicAWSCredentials(options.AccessKey, options.SecretKey),
            new AmazonS3Config
            {
                ServiceURL = options.ServiceUrl,
                AuthenticationRegion = options.Region,
                ForcePathStyle = true,
                RetryMode = RequestRetryMode.Standard,
                MaxErrorRetry = 3,
            }));
        services.AddSingleton<IObjectStorage, S3ObjectStorage>();
        return services;
    }
}
