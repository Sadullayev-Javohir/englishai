using System.Net;
using System.Text;
using FluentAssertions;
using Infrastructure.Video;
using Xunit;

namespace Integration.Tests.Video;

public class YouTubeDataApiFeedSourceTests
{
    private sealed class StubHandler : HttpMessageHandler
    {
        public bool FailAvatars { get; init; }
        public List<string> RequestedUrls { get; } = new();

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            RequestedUrls.Add(url);
            if (FailAvatars && url.Contains("/channels?", StringComparison.Ordinal))
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.ServiceUnavailable));
            var json = url.Contains("/search?", StringComparison.Ordinal) ? """
                {"nextPageToken":"next","items":[
                  {"id":{"videoId":"abcdefghijk"},"snippet":{"title":"Captioned lesson","channelTitle":"Learning Channel","channelId":"channel-1"}},
                  {"id":{"videoId":"lmnopqrstuv"},"snippet":{"title":"Second lesson","channelTitle":"Learning Channel","channelId":"channel-1"}}
                ]}
                """ : url.Contains("/channels?", StringComparison.Ordinal) ? """
                {"items":[{"id":"channel-1","snippet":{"thumbnails":{"default":{"url":"https://yt3.ggpht.com/small"},"medium":{"url":"https://yt3.ggpht.com/avatar"}}}}]}
                """ : """
                {"items":[{"id":"abcdefghijk","contentDetails":{"duration":"PT4M5S"}},{"id":"lmnopqrstuv","contentDetails":{"duration":"PT3M"}}]}
                """;

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            });
        }
    }

    [Fact]
    public async Task Search_requests_closed_captions_and_marks_every_result_as_captioned()
    {
        var handler = new StubHandler();
        var source = new YouTubeDataApiFeedSource(
            new HttpClient(handler), new YouTubeOptions { ApiKey = "test-key" });

        var page = await source.SearchAsync("travel english", null, 12, CancellationToken.None);

        handler.RequestedUrls[0].Should().Contain("videoCaption=closedCaption");
        page.Items.Should().HaveCount(2);
        page.Items[0].HasClosedCaptions.Should().BeTrue();
        page.Items[0].DurationSeconds.Should().Be(245);
        page.NextContinuation.Should().Be("next");
        page.Items.Should().OnlyContain(item => item.ChannelAvatarUrl == "https://yt3.ggpht.com/avatar");
        handler.RequestedUrls.Where(url => url.Contains("/channels?", StringComparison.Ordinal))
            .Should().ContainSingle().Which.Should().Contain("id=channel-1&");
    }

    [Fact]
    public async Task Optional_channel_lookup_failure_does_not_discard_playable_results()
    {
        var source = new YouTubeDataApiFeedSource(
            new HttpClient(new StubHandler { FailAvatars = true }), new YouTubeOptions { ApiKey = "test-key" });
        var page = await source.SearchAsync("english grammar", null, 12, CancellationToken.None);
        page.Items.Should().HaveCount(2);
        page.Items.Should().OnlyContain(item => item.ChannelAvatarUrl == null);
    }
}
