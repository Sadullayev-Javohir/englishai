using FluentAssertions;
using Infrastructure.Speaking;
using Xunit;

namespace Integration.Tests.Speaking;

public class AzureVoiceLiveConnectionBrokerTests
{
    [Fact]
    public void Parses_host_and_project_from_the_foundry_endpoint()
    {
        var (host, project) = AzureVoiceLiveConnectionBroker.ParseEndpoint(
            "https://javohirsadullayev2024-2-resource.services.ai.azure.com/api/projects/javohirsadullayev2024-2939");

        host.Should().Be("javohirsadullayev2024-2-resource.services.ai.azure.com");
        project.Should().Be("javohirsadullayev2024-2939");
    }

    [Fact]
    public void Builds_the_voice_live_agent_websocket_url()
    {
        var url = AzureVoiceLiveConnectionBroker.BuildWebSocketUrl(
            "javohirsadullayev2024-2-resource.services.ai.azure.com",
            "2026-06-01-preview",
            "2",
            "javohirsadullayev2024-2939");

        url.Should().Be(
            "wss://javohirsadullayev2024-2-resource.services.ai.azure.com/voice-live/realtime"
            + "?api-version=2026-06-01-preview&agent-name=2&agent-project-name=javohirsadullayev2024-2939");
    }

    [Fact]
    public void Endpoint_without_a_project_segment_is_rejected()
    {
        var act = () => AzureVoiceLiveConnectionBroker.ParseEndpoint(
            "https://javohirsadullayev2024-2-resource.services.ai.azure.com/api");

        act.Should().Throw<InvalidOperationException>();
    }
}
