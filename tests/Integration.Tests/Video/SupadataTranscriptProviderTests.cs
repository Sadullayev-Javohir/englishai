using System.Net;
using Application.Video.Models;
using FluentAssertions;
using Infrastructure.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the Supadata transcript provider's HTTP-to-outcome mapping with a stub handler (no real
/// network): a real body becomes <see cref="TranscriptFetchOutcome.Fetched"/>, an explicit
/// "no transcript" error is terminal <see cref="TranscriptFetchOutcome.NoCaptions"/>, and every other
/// failure (bad key, rate limit, server error, async job, no key) stays
/// <see cref="TranscriptFetchOutcome.ProviderUnavailable"/> so the fill retries (docs/development-guide.md rule 8).
/// </summary>
public class SupadataTranscriptProviderTests
{
    private const string VideoId = "dQw4w9WgXcQ";

    private static SupadataTranscriptProvider Build(HttpStatusCode status, string body, string apiKey = "k")
    {
        var http = new HttpClient(new StubHandler(status, body));
        var options = new SupadataOptions { ApiKey = apiKey };
        return new SupadataTranscriptProvider(http, options, NullLogger<SupadataTranscriptProvider>.Instance);
    }

    [Fact]
    public async Task Maps_a_real_content_body_to_fetched_lines()
    {
        var provider = Build(HttpStatusCode.OK, """
            {"content":[
              {"text":"Hello there.","offset":1000,"duration":900,"lang":"en"},
              {"text":"Welcome back.","offset":2000,"duration":900,"lang":"en"}
            ],"lang":"en","availableLangs":["en"]}
            """);

        var result = await provider.FetchAsync(VideoId);

        result.Outcome.Should().Be(TranscriptFetchOutcome.Fetched);
        result.Lines.Should().HaveCount(2);
        result.Lines[0].EnglishText.Should().Be("Hello there.");
    }

    [Fact]
    public async Task Maps_an_empty_transcript_to_no_captions()
    {
        var provider = Build(HttpStatusCode.OK, """{"content":[],"lang":"en"}""");

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.NoCaptions);
    }

    [Fact]
    public async Task Maps_an_explicit_no_transcript_error_to_no_captions()
    {
        var provider = Build(HttpStatusCode.NotFound, """{"error":"transcript-unavailable","message":"..."}""");

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.NoCaptions);
    }

    [Theory]
    [InlineData(HttpStatusCode.Unauthorized, """{"error":"invalid-api-key"}""")]
    [InlineData(HttpStatusCode.TooManyRequests, """{"error":"rate-limit-exceeded"}""")]
    [InlineData(HttpStatusCode.InternalServerError, "")]
    [InlineData(HttpStatusCode.PaymentRequired, """{"error":"quota-exceeded"}""")]
    public async Task Keeps_transient_failures_as_provider_unavailable(HttpStatusCode status, string body)
    {
        // None of these mean "this video has no captions", so they must NOT poison the lesson terminal.
        var provider = Build(status, body);

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
    }

    [Fact]
    public async Task Treats_an_async_job_response_as_pending()
    {
        // A very large transcript comes back as an async job we do not poll: retry, never terminal.
        var provider = Build(HttpStatusCode.OK, """{"jobId":"abc-123"}""");

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
    }

    [Fact]
    public async Task Is_unavailable_when_no_api_key_is_configured()
    {
        var provider = Build(HttpStatusCode.OK, """{"content":[]}""", apiKey: "");

        (await provider.FetchAsync(VideoId)).Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
    }

    /// <summary>Returns a fixed status/body for any request, so the provider is tested without a network.</summary>
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
            Task.FromResult(new HttpResponseMessage(_status) { Content = new StringContent(_body) });
    }
}
