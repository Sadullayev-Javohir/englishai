using Application.Video.FindFullVideoPlaylist;
using Application.Video.Models;
using Application.Video.Ports;
using FluentAssertions;
using NSubstitute;

namespace Application.Tests.Video;

public sealed class FullVideoPlaylistQueryHandlerTests
{
    [Fact]
    public async Task Search_maps_fresh_playlist_and_its_playable_items()
    {
        var source = Substitute.For<IVideoPlaylistDiscoverySource>();
        source.SearchAsync("Puss in Boots", 8, Arg.Any<CancellationToken>()).Returns(new[]
        {
            new VideoPlaylist("PL-puss", "Puss in Boots full movie", "Official", "cover", new[]
            {
                new VideoPlaylistItem("abcdefghijk", "Part 1", "Official", 3600, "thumb"),
            }),
        });
        var handler = new FindFullVideoPlaylistQueryHandler(source);

        var result = await handler.Handle(new FindFullVideoPlaylistQuery("Puss in Boots"), CancellationToken.None);

        result.Items.Should().ContainSingle();
        result.Items[0].Items[0].YouTubeVideoId.Should().Be("abcdefghijk");
        result.Items[0].TotalDurationSeconds.Should().Be(3600);
    }

    [Fact]
    public async Task Featured_rotates_to_next_query_when_current_source_is_empty()
    {
        var source = Substitute.For<IVideoPlaylistDiscoverySource>();
        source.SearchAsync(Arg.Any<string>(), 1, Arg.Any<CancellationToken>())
            .Returns(Task.FromResult<IReadOnlyList<VideoPlaylist>>(Array.Empty<VideoPlaylist>()));
        source.SearchAsync(Arg.Is<string>(query => query.Contains("Puss in Boots")), 1, Arg.Any<CancellationToken>())
            .Returns(new[] { new VideoPlaylist("PL-puss", "Puss in Boots", "Official", null, new[] { new VideoPlaylistItem("abcdefghijk", "Part", "Official", 3600) }) });
        var handler = new FindFullVideoPlaylistQueryHandler(source);

        var result = await handler.Handle(new GetFeaturedVideoPlaylistQuery(), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Id.Should().Be("PL-puss");
    }

    [Fact]
    public async Task Featured_uses_a_different_discovery_query_for_the_next_refresh()
    {
        var source = Substitute.For<IVideoPlaylistDiscoverySource>();
        var call = 0;
        source.SearchAsync(Arg.Any<string>(), 1, Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                call++;
                return new[]
                {
                    new VideoPlaylist(
                        $"PL-featured-{call}",
                        $"Featured collection {call}",
                        "Official",
                        null,
                        new[] { new VideoPlaylistItem("abcdefghijk", "Part", "Official", 3600) }),
                };
            });
        var handler = new FindFullVideoPlaylistQueryHandler(source);

        var first = await handler.Handle(new GetFeaturedVideoPlaylistQuery(), CancellationToken.None);
        var second = await handler.Handle(new GetFeaturedVideoPlaylistQuery(), CancellationToken.None);

        first!.Id.Should().NotBe(second!.Id);
        await source.Received(2).SearchAsync(Arg.Any<string>(), 1, Arg.Any<CancellationToken>());
    }
}
