using System.Net;
using System.Text;
using Application.Video.Models;
using FluentAssertions;
using Infrastructure.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the keyless transcript provider end-to-end against captured-shape fixtures (no real
/// network): it reads the watch page, parses the <c>get_transcript</c> response, falls back to
/// <c>timedtext</c> json3, and stays empty (honest "pending") when captions are unavailable.
/// </summary>
public class YouTubeTranscriptProviderTests
{
    private const string WatchHtml = """
        <html><head><script>
        var ytcfg = {"INNERTUBE_API_KEY":"test-key","INNERTUBE_CONTEXT_CLIENT_VERSION":"2.2026.00.00"};
        </script><script>
        var data = {"getTranscriptEndpoint":{"params":"UEElM0QlM0Q"},
        "captions":{"playerCaptionsTracklistRenderer":{"captionTracks":[{"baseUrl":"https://www.youtube.com/api/timedtext?v=x&lang=en","languageCode":"en"}]}}};
        </script></head><body></body></html>
        """;

    private const string GetTranscriptJson = """
        {"actions":[{"updateEngagementPanelAction":{"content":{"transcriptRenderer":{"content":
        {"transcriptSearchPanelRenderer":{"body":{"transcriptSegmentListRenderer":{"initialSegments":[
          {"transcriptSegmentRenderer":{"startMs":"0","endMs":"2000","snippet":{"runs":[{"text":"Hello there."}]}}},
          {"transcriptSegmentRenderer":{"startMs":"2000","endMs":"4500","snippet":{"runs":[{"text":"Welcome to the lesson."}]}}}
        ]}}}}}}}}]}
        """;

    private const string EmptyGetTranscriptJson = """{"actions":[]}""";

    private const string TimedTextJson3 = """
        {"events":[
          {"tStartMs":0,"dDurationMs":2000,"segs":[{"utf8":"Hello"},{"utf8":" world."}]},
          {"tStartMs":2000,"dDurationMs":2500,"segs":[{"utf8":"Second line."}]},
          {"tStartMs":5000,"dDurationMs":500,"segs":[{"utf8":"\n"}]}
        ]}
        """;

    // Some real json3 responses type the offsets as strings ("0") rather than numbers.
    private const string TimedTextJson3StringOffsets = """
        {"events":[
          {"tStartMs":"0","dDurationMs":"2000","segs":[{"utf8":"Hello"},{"utf8":" world."}]},
          {"tStartMs":"2000","dDurationMs":"2500","segs":[{"utf8":"Second line."}]}
        ]}
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        private readonly string _getTranscriptBody;
        private readonly string? _watchHtml;
        private readonly string _timedTextBody;

        public StubHandler(string getTranscriptBody, string? watchHtml = WatchHtml, string? timedTextBody = null)
        {
            _getTranscriptBody = getTranscriptBody;
            _watchHtml = watchHtml;
            _timedTextBody = timedTextBody ?? TimedTextJson3;
        }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            var body = request.Method == HttpMethod.Post && url.Contains("get_transcript")
                ? _getTranscriptBody
                : url.Contains("timedtext")
                    ? _timedTextBody
                    : _watchHtml ?? "";

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(body, Encoding.UTF8, "text/plain"),
            });
        }
    }

    private static YouTubeTranscriptProvider Provider(StubHandler handler) =>
        new(new HttpClient(handler), NullLogger<YouTubeTranscriptProvider>.Instance);

    [Fact]
    public async Task Parses_get_transcript_segments()
    {
        var provider = Provider(new StubHandler(GetTranscriptJson));

        var lines = (await provider.FetchAsync("vid", CancellationToken.None)).Lines;

        lines.Should().HaveCount(2);
        lines[0].StartSeconds.Should().Be(0);
        lines[0].EndSeconds.Should().Be(2);
        lines[0].EnglishText.Should().Be("Hello there.");
        lines[1].EnglishText.Should().Be("Welcome to the lesson.");
    }

    [Fact]
    public async Task Falls_back_to_timedtext_when_get_transcript_is_empty()
    {
        var provider = Provider(new StubHandler(EmptyGetTranscriptJson));

        var lines = (await provider.FetchAsync("vid", CancellationToken.None)).Lines;

        lines.Should().HaveCount(2, "the whitespace-only third event is dropped");
        lines[0].EnglishText.Should().Be("Hello world.");
        lines[1].EnglishText.Should().Be("Second line.");
    }

    [Fact]
    public async Task Parses_timedtext_when_offsets_are_strings_instead_of_throwing()
    {
        // Regression: a string-typed tStartMs once surfaced an InvalidOperationException that
        // escaped the provider and turned the lesson read into a 500. It must parse cleanly now.
        var provider = Provider(new StubHandler(EmptyGetTranscriptJson, timedTextBody: TimedTextJson3StringOffsets));

        var lines = (await provider.FetchAsync("vid", CancellationToken.None)).Lines;

        // The string offsets must parse cleanly: the first sentence starts at the string "0" and the
        // second at the string "2000" (2s) - confirming the tolerant numeric read, no exception.
        lines.Should().HaveCount(2);
        lines[0].StartSeconds.Should().Be(0);
        lines[0].EnglishText.Should().Be("Hello world.");
        lines[1].StartSeconds.Should().Be(2);
    }

    [Fact]
    public async Task Returns_empty_when_timedtext_payload_is_malformed()
    {
        // Any unexpected/garbage payload must yield an honest empty transcript, never an exception.
        var provider = Provider(new StubHandler(EmptyGetTranscriptJson, timedTextBody: "not json at all"));

        var lines = (await provider.FetchAsync("vid", CancellationToken.None)).Lines;

        lines.Should().BeEmpty();
    }

    [Fact]
    public async Task Returns_empty_when_no_transcript_surface_is_present()
    {
        // Watch page with neither transcript params nor caption tracks → honest empty.
        var provider = Provider(new StubHandler(EmptyGetTranscriptJson, watchHtml: "<html><body>no captions</body></html>"));

        var lines = (await provider.FetchAsync("vid", CancellationToken.None)).Lines;

        lines.Should().BeEmpty();
    }
}
