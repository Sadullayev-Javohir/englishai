using System.Net;
using Application.Video.Models;
using FluentAssertions;
using Infrastructure.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the youtubei.js sidecar provider's response-to-outcome mapping with a stub handler (no real
/// network): <c>found:true</c> + lines → Fetched, <c>found:false</c> → terminal NoCaptions, a non-2xx /
/// unconfigured base URL → ProviderUnavailable so the orchestrator falls through (docs/development-guide.md rule 8).
/// </summary>
public class YoutubeiTranscriptProviderTests
{
    private const string VideoId = "dQw4w9WgXcQ";

    private static YoutubeiTranscriptProvider Build(HttpStatusCode status, string body, string? baseUrl = "http://sidecar")
    {
        var http = new HttpClient(new StubHandler(status, body));
        var options = new YoutubeiOptions { BaseUrl = baseUrl };
        return new YoutubeiTranscriptProvider(http, options, NullLogger<YoutubeiTranscriptProvider>.Instance);
    }

    [Fact]
    public async Task Maps_found_lines_to_fetched()
    {
        var provider = Build(HttpStatusCode.OK, """
            {"found":true,"lines":[
              {"start":0.0,"end":2.0,"text":"Hello there."},
              {"start":2.0,"end":4.0,"text":"Welcome back."}
            ]}
            """);

        var result = await provider.FetchAsync(VideoId);

        result.Outcome.Should().Be(TranscriptFetchOutcome.Fetched);
        result.Lines.Should().HaveCount(2);
        result.Lines[0].EnglishText.Should().Be("Hello there.");
    }

    [Fact]
    public async Task Maps_found_false_to_no_captions()
    {
        var provider = Build(HttpStatusCode.OK, """{"found":false,"reason":"no-transcript-panel"}""");

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.NoCaptions);
    }

    [Fact]
    public async Task Maps_a_server_error_to_provider_unavailable()
    {
        var provider = Build(HttpStatusCode.BadGateway, """{"error":"fetch failed"}""");

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
    }

    [Fact]
    public async Task Is_unavailable_when_not_configured()
    {
        var provider = Build(HttpStatusCode.OK, "{}", baseUrl: null);

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
    }

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly HttpStatusCode _status;
        private readonly string _body;

        public StubHandler(HttpStatusCode status, string body)
        {
            _status = status;
            _body = body;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken) =>
            Task.FromResult(new HttpResponseMessage(_status)
            {
                Content = new StringContent(_body, System.Text.Encoding.UTF8, "application/json"),
            });
    }
}
