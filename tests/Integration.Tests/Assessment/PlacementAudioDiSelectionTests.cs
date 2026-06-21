using Application.Assessment.Ports;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Assessment;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Assessment;

public sealed class PlacementAudioDiSelectionTests
{
    [Fact]
    public void Database_with_disabled_object_storage_uses_in_memory_cache()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "Host=localhost;Database=englishai;Username=test;Password=test",
            ["Postgres:PoolSize"] = "16",
            ["Postgres:ServerConnectionBudget"] = "16",
            ["ObjectStorage:Enabled"] = "false",
        });

        provider.GetRequiredService<IPlacementAudioCache>()
            .Should().BeOfType<InMemoryPlacementAudioCache>();
    }

    [Fact]
    public void Enabled_object_storage_uses_object_cache()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["ObjectStorage:Enabled"] = "true",
            ["ObjectStorage:ServiceUrl"] = "https://storage.example.com",
            ["ObjectStorage:Region"] = "fsn1",
            ["ObjectStorage:AccessKey"] = "access-key",
            ["ObjectStorage:SecretKey"] = "secret-key",
            ["ObjectStorage:PublicBucket"] = "public",
            ["ObjectStorage:PrivateBucket"] = "private",
            ["ObjectStorage:PublicBaseUrl"] = "https://cdn.example.com",
        });

        using var scope = provider.CreateScope();
        scope.ServiceProvider.GetRequiredService<IPlacementAudioCache>()
            .Should().BeOfType<ObjectPlacementAudioCache>();
    }

    [Fact]
    public void Without_database_or_object_storage_uses_in_memory_cache()
    {
        using var provider = Build(new Dictionary<string, string?>());

        provider.GetRequiredService<IPlacementAudioCache>()
            .Should().BeOfType<InMemoryPlacementAudioCache>();
    }

    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }
}
