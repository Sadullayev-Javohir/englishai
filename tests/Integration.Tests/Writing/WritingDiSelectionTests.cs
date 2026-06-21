using Application.Writing.Ports;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Writing;
using Integration.Tests.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Writing;

public class WritingDiSelectionTests
{
    private static ServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    [Fact]
    public void Without_a_gateway_key_the_offline_assessor_is_used()
    {
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        using var provider = Build(new Dictionary<string, string?>());

        provider.GetRequiredService<ITopicWritingAssessor>().Should().BeOfType<LocalWritingAssessor>();
        provider.GetRequiredService<IWritingAssessor>().Should().BeOfType<LocalWritingAssessor>();
    }

    [Fact]
    public void With_a_gateway_key_topic_writing_uses_ai_and_the_fallback_port_stays_local()
    {
        using var provider = Build(new Dictionary<string, string?>
        {
            ["HermesGateway:ApiKey"] = "test-key",
        });

        provider.GetRequiredService<ITopicWritingAssessor>().Should().BeOfType<ResilientWritingAssessor>();
        provider.GetRequiredService<IWritingAssessor>().Should().BeOfType<LocalWritingAssessor>();
    }
}
