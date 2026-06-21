using Application.Speaking.Ports;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Speaking;
using Integration.Tests.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Speaking;

/// <summary>
/// Verifies the config-gated adapter selection: Local adapters when no keys are configured,
/// Azure speech when its keys exist, and the LLM tutor when the Hermes gateway - the only LLM backend
/// - is configured.
/// </summary>
public class SpeakingDiSelectionTests
{
    private static IServiceProvider Build(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(settings)
            .Build();

        return new ServiceCollection()
            .AddInfrastructure(configuration)
            .BuildServiceProvider();
    }

    [Fact]
    public void Without_keys_local_adapters_are_used()
    {
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        var provider = Build(new Dictionary<string, string?>());

        provider.GetRequiredService<ISpeechToTextService>().Should().BeOfType<LocalSpeechToTextService>();
        provider.GetRequiredService<IPronunciationAssessor>().Should().BeOfType<LocalPronunciationAssessor>();
        provider.GetRequiredService<ITextToSpeechService>().Should().BeOfType<LocalTextToSpeechService>();
        provider.GetRequiredService<IConversationTutor>().Should().BeOfType<LocalConversationTutor>();
    }

    [Fact]
    public void With_azure_keys_azure_speech_adapters_are_used()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureSpeech:Key"] = "test-key",
            ["AzureSpeech:Region"] = "westeurope",
        });

        provider.GetRequiredService<ISpeechToTextService>().Should().BeOfType<AzureSpeechToTextService>();
        provider.GetRequiredService<IPronunciationAssessor>().Should().BeOfType<AzurePronunciationAssessor>();
        // Synthesis goes through the cache so a repeated phrase is paid for once (docs/development-guide.md rule 10).
        provider.GetRequiredService<ITextToSpeechService>().Should().BeOfType<CachedTextToSpeechService>();
    }

    [Fact]
    public void Streaming_speech_resolves_without_downcasting_the_decorated_port()
    {
        // The streaming session belongs to the concrete Azure adapter. Reaching it by casting
        // ISpeechToTextService would throw as soon as anything decorates that port.
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureSpeech:Key"] = "test-key",
            ["AzureSpeech:Region"] = "westeurope",
        });

        provider.GetRequiredService<IStreamingSpeechToTextService>()
            .Should().BeOfType<AzureSpeechToTextService>();
    }

    [Fact]
    public void Whisper_configured_routes_transcription_by_tier()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureSpeech:Key"] = "test-key",
            ["AzureSpeech:Region"] = "westeurope",
            ["WhisperStt:Enabled"] = "true",
        });

        provider.GetRequiredService<ISpeechToTextService>().Should().BeOfType<TieredSpeechToTextService>();
        // Streaming has no sidecar contract and only the accent-tutor hub uses it, so it stays Azure.
        provider.GetRequiredService<IStreamingSpeechToTextService>()
            .Should().BeOfType<AzureSpeechToTextService>();
    }

    [Fact]
    public void Without_whisper_every_learner_stays_on_the_metered_provider()
    {
        // The safe default: an unconfigured sidecar costs money but works.
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureSpeech:Key"] = "test-key",
            ["AzureSpeech:Region"] = "westeurope",
        });

        provider.GetRequiredService<ISpeechToTextService>().Should().BeOfType<AzureSpeechToTextService>();
    }

    [Fact]
    public void Cached_synthesis_still_exposes_the_named_voice_the_accent_tutors_need()
    {
        // AccentTutorVoiceService asks for IVoicedTextToSpeechService. If the cache decorator did
        // not carry that capability forward, all four accent tutors would silently fall back to the
        // single default voice - visible to learners, invisible in logs.
        var provider = Build(new Dictionary<string, string?>
        {
            ["AzureSpeech:Key"] = "test-key",
            ["AzureSpeech:Region"] = "westeurope",
        });

        provider.GetRequiredService<IVoicedTextToSpeechService>()
            .Should().BeOfType<CachedTextToSpeechService>();
        provider.GetRequiredService<ITextToSpeechService>()
            .Should().BeSameAs(provider.GetRequiredService<IVoicedTextToSpeechService>());
    }

    [Fact]
    public void With_hermes_key_llm_tutor_is_used()
    {
        var provider = Build(new Dictionary<string, string?>
        {
            ["HermesGateway:ApiKey"] = "test-key",
        });

        provider.GetRequiredService<IConversationTutor>().Should().BeOfType<ResilientConversationTutor>();
    }

    [Fact]
    public void Leftover_third_party_provider_keys_do_not_enable_the_llm_tutor()
    {
        // Groq, OpenRouter and Google AI Studio were removed on 2026-07-28 - the Hermes gateway is the
        // only LLM backend. A key for a removed provider left behind in config or env must select
        // NOTHING: an operator who forgets to delete it should get the honest Local tutor, never a
        // silent attempt to reach a provider this codebase no longer speaks to (docs/development-guide.md rules 8, 10).
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        var provider = Build(new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "stale-key",
            ["Groq:ApiKey"] = "stale-key",
            ["GoogleAiStudio:ApiKey"] = "stale-key",
        });

        provider.GetRequiredService<IConversationTutor>().Should().BeOfType<LocalConversationTutor>();
    }
}
