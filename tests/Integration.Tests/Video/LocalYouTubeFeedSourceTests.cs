using System.Net;
using System.Text;
using FluentAssertions;
using Infrastructure.Video;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the keyless feed source parses YouTube's public search HTML (first page) and the
/// youtubei continuation JSON (later pages), filters out Shorts, and round-trips the
/// continuation token - all against captured-shape fixtures, with no real network call.
/// </summary>
public class LocalYouTubeFeedSourceTests
{
    private const string ResultsHtml = """
        <html><head><script>
        var ytcfg = {"INNERTUBE_API_KEY":"test-key","INNERTUBE_CONTEXT_CLIENT_VERSION":"2.2026.00.00"};
        </script><script>
        var ytInitialData = {"contents":{"items":[
          {"videoRenderer":{"videoId":"aaaaaaaaaaa","lengthText":{"simpleText":"3:32"},
            "title":{"runs":[{"text":"Learn English A2"}]},"ownerText":{"runs":[{"text":"BBC Learning"}]},
            "thumbnail":{"thumbnails":[{"url":"https://i.ytimg.com/video.jpg"}]},
            "channelThumbnailSupportedRenderers":{"channelThumbnailWithLinkRenderer":{"thumbnail":{"thumbnails":[
              {"url":"https://yt3.ggpht.com/channel-small","width":32},
              {"url":"https://yt3.ggpht.com/channel-avatar","width":68}]}}},
            "badges":[{"metadataBadgeRenderer":{"label":"CC","accessibilityData":{"label":"Closed captions"}}}]}},
          {"videoRenderer":{"videoId":"bbbbbbbbbbb","lengthText":{"simpleText":"0:15"},
            "title":{"runs":[{"text":"Short clip"}]},"ownerText":{"runs":[{"text":"Shorts Channel"}]}}},
          {"videoRenderer":{"videoId":"ddddddddddd","lengthText":{"simpleText":"5:10"},
            "title":{"runs":[{"text":"Lesson without captions"}]},"ownerText":{"runs":[{"text":"Channel D"}]}}},
          {"continuationItemRenderer":{"continuationEndpoint":{"continuationCommand":{"token":"NEXT_TOKEN"}}}}
        ]}};</script></head><body></body></html>
        """;

    private const string ContinuationJson = """
        {"onResponseReceivedCommands":[{"appendContinuationItemsAction":{"continuationItems":[
          {"itemSectionRenderer":{"contents":[
            {"videoRenderer":{"videoId":"ccccccccccc","lengthText":{"simpleText":"4:00"},
              "title":{"runs":[{"text":"Next page video"}]},"ownerText":{"runs":[{"text":"Channel C"}]},
              "channelThumbnail":{"thumbnails":[{"url":"//yt3.ggpht.com/channel-c","width":68}]},
              "badges":[{"metadataBadgeRenderer":{"accessibilityData":{"label":"Subtitles/closed captions"}}}]}}
          ]}},
          {"continuationItemRenderer":{"continuationEndpoint":{"continuationCommand":{"token":"TOKEN_2"}}}}
        ]}}]}
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        public string? LastPostUrl { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            string content;
            if (request.Method == HttpMethod.Post)
            {
                LastPostUrl = url;
                content = ContinuationJson;
            }
            else
            {
                content = ResultsHtml;
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/html"),
            });
        }
    }

    [Fact]
    public async Task First_page_parses_results_filters_shorts_and_exposes_a_cursor()
    {
        var source = new LocalYouTubeFeedSource(new HttpClient(new StubHandler()));

        var page = await source.SearchAsync("learn english a2", null, 12, CancellationToken.None);

        page.Items.Should().ContainSingle("Shorts and videos without a confirmed CC badge are filtered out");
        page.Items[0].YouTubeVideoId.Should().Be("aaaaaaaaaaa");
        page.Items[0].HasClosedCaptions.Should().BeTrue();
        page.Items[0].Title.Should().Be("Learn English A2");
        page.Items[0].Channel.Should().Be("BBC Learning");
        page.Items[0].ChannelAvatarUrl.Should().Be("https://yt3.ggpht.com/channel-avatar");
        page.Items[0].DurationSeconds.Should().Be(212); // 3:32
        page.NextContinuation.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task Continuation_page_posts_to_youtubei_and_parses_next_items()
    {
        var handler = new StubHandler();
        var source = new LocalYouTubeFeedSource(new HttpClient(handler));

        var first = await source.SearchAsync("learn english a2", null, 12, CancellationToken.None);
        var second = await source.SearchAsync("learn english a2", first.NextContinuation, 12, CancellationToken.None);

        handler.LastPostUrl.Should().Contain("youtubei/v1/search").And.Contain("key=test-key");
        second.Items.Should().ContainSingle();
        second.Items[0].YouTubeVideoId.Should().Be("ccccccccccc");
        second.Items[0].ChannelAvatarUrl.Should().Be("https://yt3.ggpht.com/channel-c");
        second.NextContinuation.Should().NotBeNullOrEmpty();
    }
}
