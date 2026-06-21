using Application.Assistant.Ports;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Assistant;
using Integration.Tests.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Assistant;

public class AssistantDiSelectionTests
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
    public void Without_any_llm_provider_the_no_op_assistant_is_used()
    {
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        using var provider = Build(new Dictionary<string, string?>());

        provider.GetRequiredService<ILearningAssistant>().Should().BeOfType<NoOpLearningAssistant>();
    }

    [Fact]
    public void Leftover_third_party_provider_keys_do_not_enable_the_learning_assistant()
    {
        // Groq, OpenRouter and Google AI Studio were removed on 2026-07-28 - the Hermes gateway is the
        // only LLM backend. Without the gateway there is no assistant, whatever stale third-party keys
        // remain in config: the honest no-op beats an assistant with nothing behind it (rules 8, 10).
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        using var provider = Build(new Dictionary<string, string?>
        {
            ["Groq:ApiKey"] = "stale-key",
            ["Gemini:ApiKey"] = "stale-key",
            ["GoogleAiStudio:ApiKey"] = "stale-key",
        });

        provider.GetRequiredService<ILearningAssistant>().Should().BeOfType<NoOpLearningAssistant>();
    }
}
