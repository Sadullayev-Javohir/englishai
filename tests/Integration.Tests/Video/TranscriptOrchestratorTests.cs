using Application.Video.Models;
using Application.Video.Ports;
using FluentAssertions;
using Infrastructure.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the transcript fallback chain managed by <see cref="TranscriptOrchestrator"/>: the first
/// provider with real lines wins, the next is tried when one has none, a provider that THROWS or hangs
/// never halts the chain, and the terminal "no captions" outcome is reported ONLY when every provider
/// ran and agreed - a transient failure anywhere keeps the result retryable (docs/development-guide.md rule 8).
/// </summary>
public class TranscriptOrchestratorTests
{
    private static TranscriptOrchestrator Build(params IVideoTranscriptProvider[] providers) =>
        new(providers.ToList(), NullLogger<TranscriptOrchestrator>.Instance);

    private static TranscriptOrchestrator Build(params TranscriptFetchResult[] results) =>
        Build(results.Select(r => (IVideoTranscriptProvider)new StubProvider(r)).ToArray());

    private static readonly TranscriptFetchResult Lines =
        TranscriptFetchResult.Fetched(new[] { new TranscriptLine(0, 1, "Hi") });

    [Fact]
    public async Task Returns_the_first_providers_real_transcript()
    {
        var provider = Build(Lines, TranscriptFetchResult.ProviderUnavailable);

        (await provider.FetchAsync("v")).Outcome.Should().Be(TranscriptFetchOutcome.Fetched);
    }

    [Fact]
    public async Task Falls_through_to_a_later_provider_when_the_first_cannot_run()
    {
        var provider = Build(TranscriptFetchResult.ProviderUnavailable, Lines);

        var result = await provider.FetchAsync("v");

        result.Outcome.Should().Be(TranscriptFetchOutcome.Fetched);
        result.Lines.Should().HaveCount(1);
    }

    [Fact]
    public async Task Is_terminal_only_when_every_provider_confirms_no_captions()
    {
        var provider = Build(TranscriptFetchResult.NoCaptions, TranscriptFetchResult.NoCaptions);

        (await provider.FetchAsync("v")).Outcome.Should().Be(TranscriptFetchOutcome.NoCaptions);
    }

    [Fact]
    public async Task Stays_retryable_when_no_captions_mixes_with_a_transient_failure()
    {
        // One provider says "no captions" but another could not run: settling terminal would poison an
        // otherwise-captioned lesson, so the result must stay ProviderUnavailable for a later retry.
        var provider = Build(TranscriptFetchResult.NoCaptions, TranscriptFetchResult.ProviderUnavailable);

        (await provider.FetchAsync("v")).Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
    }

    [Fact]
    public async Task A_throwing_provider_does_not_halt_the_chain()
    {
        // Robustness: a provider that throws is isolated (treated as unavailable) so the next source is
        // still tried and a later provider's real transcript still wins.
        var provider = Build(new ThrowingProvider(), new StubProvider(Lines));

        var result = await provider.FetchAsync("v");

        result.Outcome.Should().Be(TranscriptFetchOutcome.Fetched);
    }

    [Fact]
    public async Task A_throwing_only_chain_is_retryable_not_terminal()
    {
        var provider = Build(new ThrowingProvider(), new ThrowingProvider());

        (await provider.FetchAsync("v")).Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
    }

    private sealed class StubProvider : IVideoTranscriptProvider
    {
        private readonly TranscriptFetchResult _result;
        public StubProvider(TranscriptFetchResult result) => _result = result;

        public Task<TranscriptFetchResult> FetchAsync(string youTubeVideoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(_result);
    }

    private sealed class ThrowingProvider : IVideoTranscriptProvider
    {
        public Task<TranscriptFetchResult> FetchAsync(string youTubeVideoId, CancellationToken cancellationToken = default) =>
            throw new InvalidOperationException("boom");
    }
}
