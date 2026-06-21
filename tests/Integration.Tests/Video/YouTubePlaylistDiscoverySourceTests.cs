using System.Net;
using System.Text;
using FluentAssertions;
using Infrastructure.Video;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// YouTube's current web search and playlist pages use lockupViewModel rather than the
/// legacy playlistRenderer/playlistVideoRenderer nodes. Keep the keyless fallback aligned
/// with that shape without needing an API key or a live network request in tests.
/// </summary>
public class YouTubePlaylistDiscoverySourceTests
{
    private const string SearchHtml = """
        <html><head><script>
        var ytInitialData = {"contents":[
          {"lockupViewModel":{
            "contentType":"LOCKUP_CONTENT_TYPE_PLAYLIST",
            "contentImage":{"thumbnailViewModel":{"image":{"sources":[{"url":"https://i.ytimg.com/playlist.jpg"}]}}},
            "metadata":{"lockupMetadataViewModel":{
              "title":{"content":"Puss in Boots full movie collection"},
              "metadata":{"contentMetadataViewModel":{"metadataRows":[
                {"metadataParts":[{"text":{"content":"DreamWorks English"}}]}
              ]}}
            }},
            "rendererContext":{"commandContext":{"onTap":{"innertubeCommand":{
              "watchEndpoint":{"playlistId":"PL-puss"}
            }}}}
          }}
        ]};
        </script></head><body></body></html>
        """;

    private const string PlaylistHtml = """
        <html><head><script>
        var ytInitialData = {
          "metadata":{"playlistMetadataRenderer":{"title":"Puss in Boots full movie collection"}},
          "contents":[
            {"lockupViewModel":{
              "contentImage":{"thumbnailViewModel":{
                "image":{"sources":[{"url":"https://i.ytimg.com/vi/abcdefghijk/hqdefault.jpg"}]},
                "overlays":[{"thumbnailBottomOverlayViewModel":{"badges":[
                  {"thumbnailBadgeViewModel":{"text":"12:00"}}
                ]}}]
              }},
              "metadata":{"lockupMetadataViewModel":{
                "title":{"content":"Puss in Boots — Part 1"},
                "metadata":{"contentMetadataViewModel":{"metadataRows":[
                  {"metadataParts":[{"text":{"content":"DreamWorks English"}}]}
                ]}}
              }},
              "rendererContext":{"commandContext":{"onTap":{"innertubeCommand":{
                "watchEndpoint":{"videoId":"abcdefghijk","playlistId":"PL-puss"}
              }}}}
            }},
            {"lockupViewModel":{
              "contentImage":{"thumbnailViewModel":{
                "image":{"sources":[{"url":"https://i.ytimg.com/vi/lmnopqrstuv/hqdefault.jpg"}]},
                "overlays":[{"thumbnailBottomOverlayViewModel":{"badges":[
                  {"thumbnailBadgeViewModel":{"text":"10:00"}}
                ]}}]
              }},
              "metadata":{"lockupMetadataViewModel":{
                "title":{"content":"Puss in Boots — Part 2"},
                "metadata":{"contentMetadataViewModel":{"metadataRows":[
                  {"metadataParts":[{"text":{"content":"DreamWorks English"}}]}
                ]}}
              }},
              "rendererContext":{"commandContext":{"onTap":{"innertubeCommand":{
                "watchEndpoint":{"videoId":"lmnopqrstuv","playlistId":"PL-puss"}
              }}}}
            }}
          ]
        };
        </script></head><body></body></html>
        """;

    private sealed class StubHandler : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var content = request.RequestUri!.AbsolutePath == "/results" ? SearchHtml : PlaylistHtml;
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(content, Encoding.UTF8, "text/html"),
            });
        }
    }

    [Fact]
    public async Task Keyless_search_reads_modern_lockup_playlists_and_duration_checked_items()
    {
        var source = new YouTubePlaylistDiscoverySource(new HttpClient(new StubHandler()), new YouTubeOptions());

        var found = await source.SearchAsync("Puss in Boots", 3);

        found.Should().ContainSingle();
        var playlist = found[0];
        playlist.Id.Should().Be("PL-puss");
        playlist.Title.Should().Be("Puss in Boots full movie collection");
        playlist.Channel.Should().Be("DreamWorks English");
        playlist.Items.Should().HaveCount(2);
        playlist.Items.Select(item => item.DurationSeconds).Should().Equal(720, 600);
        playlist.TotalDurationSeconds.Should().Be(1320);
    }
}
