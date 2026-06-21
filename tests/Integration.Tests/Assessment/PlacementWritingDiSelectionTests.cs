using Application.Writing.Ports;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Writing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Assessment;

public class PlacementWritingDiSelectionTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void Placement_uses_the_configured_resilient_AI_examiner(bool configured)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Postgres"] = "",
            ["Redis:ConnectionString"] = "",
            ["HermesGateway:Endpoint"] = "http://127.0.0.1:1/v1",
            ["HermesGateway:ApiKey"] = configured ? "test-only-no-network" : "",
            ["Auth:Jwt:SigningKey"] = "test-key-not-a-real-secret-32-characters",
        }).Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        using var provider = services.BuildServiceProvider();
        var examiner = provider.GetRequiredService<IWritingAssessor>();
        if (configured) examiner.Should().BeOfType<ResilientWritingAssessor>();
        else examiner.Should().BeOfType<LocalWritingAssessor>();
    }
}
