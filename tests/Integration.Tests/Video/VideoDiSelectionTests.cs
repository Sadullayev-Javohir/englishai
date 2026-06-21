using Application.Video.Ports;
using Application.Translation.Ports;
using FluentAssertions;
using Infrastructure;
using Infrastructure.Translation;
using Infrastructure.Video;
using Integration.Tests.Llm;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the config-gated adapter selection for the Video module: Local stand-ins when
/// no keys are configured, the real YouTube adapter when its key is set, and the LLM leveler
/// when any free-provider key (Gemini or the Hermes gateway) is set (PROJECT-SPEC B.3, rule 10).
/// </summary>
public class VideoDiSelectionTests
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
    public void Without_keys_local_video_adapters_are_used()
    {
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        var provider = Build(new Dictionary<string, string?>());

        provider.GetRequiredService<IYouTubeMetadataProvider>().Should().BeOfType<LocalYouTubeMetadataProvider>();
        provider.GetRequiredService<ICefrVideoLeveler>().Should().BeOfType<LocalCefrVideoLeveler>();
        provider.GetRequiredService<IVideoRepository>().Should().BeOfType<InMemoryVideoRepository>();
        // The feed is always the caching decorator; without a key it wraps the keyless scraper.
        provider.GetRequiredService<IVideoFeedSource>().Should().BeOfType<CachingVideoFeedSource>();
        provider.GetService<LocalYouTubeFeedSource>().Should().NotBeNull();
        provider.GetService<YouTubeDataApiFeedSource>().Should().BeNull();
        // The transcript source is always the orchestrator now (it manages the chain even with a single
        // provider - yt-dlp cookie-less - when no keys are configured).
        provider.GetRequiredService<IVideoTranscriptProvider>().Should().BeOfType<TranscriptOrchestrator>();
    }

    [Fact]
    public void With_supadata_key_transcript_provider_is_the_orchestrated_chain()
    {
        // A Supadata key adds the managed API as the last link of the orchestrated chain - the source
        // that gets transcripts past the prod VPS's datacenter-IP bot wall (docs/development-guide.md rules 8, 10).
        var provider = Build(new Dictionary<string, string?> { ["Supadata:ApiKey"] = "test-key" });

        provider.GetRequiredService<IVideoTranscriptProvider>().Should().BeOfType<TranscriptOrchestrator>();
    }

    [Fact]
    public void With_youtube_key_real_metadata_provider_is_used()
    {
        var provider = Build(new Dictionary<string, string?> { ["YouTube:ApiKey"] = "test-key" });

        provider.GetRequiredService<IYouTubeMetadataProvider>().Should().BeOfType<YouTubeMetadataProvider>();
        // With a key the caching decorator wraps the official Data API source.
        provider.GetRequiredService<IVideoFeedSource>().Should().BeOfType<CachingVideoFeedSource>();
        provider.GetService<YouTubeDataApiFeedSource>().Should().NotBeNull();
    }

    [Fact]
    public void Leftover_third_party_provider_keys_do_not_enable_the_llm_video_adapters()
    {
        // Groq, OpenRouter and Google AI Studio were removed on 2026-07-28 - the Hermes gateway is the
        // only LLM backend. A stale key for a removed provider must select the deterministic Local
        // adapters, not an LLM path that has no provider behind it (docs/development-guide.md rules 8, 10).
        using var envGuard = EnvVarGuard.Clear(
            "HERMES_GATEWAY_API_KEY", "HermesGateway__ApiKey");
        var provider = Build(new Dictionary<string, string?>
        {
            ["Gemini:ApiKey"] = "stale-key",
            ["Groq:ApiKey"] = "stale-key",
            ["GoogleAiStudio:ApiKey"] = "stale-key",
        });

        provider.GetRequiredService<ICefrVideoLeveler>().Should().BeOfType<LocalCefrVideoLeveler>();
        provider.GetRequiredService<IVideoTranscriptTranslator>().Should().BeOfType<LocalTranscriptTranslator>();
        provider.GetRequiredService<IChatExplainer>().Should().BeOfType<LocalChatExplainer>();
    }

    [Fact]
    public void With_hermes_gateway_key_llm_leveler_is_used()
    {
        var provider = Build(new Dictionary<string, string?> { ["HermesGateway:ApiKey"] = "test-key" });

        provider.GetRequiredService<ICefrVideoLeveler>().Should().BeOfType<LlmCefrVideoLeveler>();
        provider.GetRequiredService<IVideoTranscriptTranslator>().Should().BeOfType<LlmTranscriptTranslator>();
        provider.GetRequiredService<IChatExplainer>().Should().BeOfType<LlmChatExplainer>();
        provider.GetRequiredService<ITextTranslator>().Should().BeOfType<ResilientTextTranslator>();
    }
}
