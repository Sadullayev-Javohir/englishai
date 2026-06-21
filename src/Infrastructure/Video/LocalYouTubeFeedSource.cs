using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Application.Video.Models;
using Application.Video.Ports;

namespace Infrastructure.Video;

/// <summary>
/// Keyless <see cref="IVideoFeedSource"/>: reads YouTube's public search results page
/// (<c>ytInitialData</c>) for the first page and its unofficial <c>youtubei/v1/search</c>
/// continuation endpoint for later pages. Used when no YouTube Data API key is configured
/// (docs/development-guide.md rule 10 config-gating); swapped for <see cref="YouTubeDataApiFeedSource"/>
/// when a key is present. The continuation token carries the api key + client version it
/// needs, so the query layer can keep treating it as opaque.
/// </summary>
public sealed class LocalYouTubeFeedSource : IVideoFeedSource
{
    // sp=EgIQAQ%3D%3D restricts results to the "Video" type (no channels/playlists/Shorts shelves).
    private const string VideoTypeFilter = "EgIQAQ%3D%3D";

    private readonly HttpClient _http;

    public LocalYouTubeFeedSource(HttpClient http)
    {
        _http = http;
        if (!_http.DefaultRequestHeaders.UserAgent.Any())
        {
            // A desktop browser UA + English locale so YouTube serves the parseable HTML page.
            _http.DefaultRequestHeaders.TryAddWithoutValidation(
                "User-Agent",
                "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 " +
                "(KHTML, like Gecko) Chrome/120.0.0.0 Safari/537.36");
            _http.DefaultRequestHeaders.TryAddWithoutValidation("Accept-Language", "en-US,en;q=0.9");
        }
    }

    public Task<VideoFeedPage> SearchAsync(
        string searchTerm, string? continuation, int pageSize, CancellationToken cancellationToken)
    {
        var state = LocalContinuation.TryDecode(continuation);
        return state is null
            ? FirstPageAsync(searchTerm, pageSize, cancellationToken)
            : NextPageAsync(state, pageSize, cancellationToken);
    }

    private async Task<VideoFeedPage> FirstPageAsync(
        string searchTerm, int pageSize, CancellationToken cancellationToken)
    {
        var url = $"https://www.youtube.com/results?search_query={Uri.EscapeDataString(searchTerm)}&sp={VideoTypeFilter}";
        using var response = await _http.GetAsync(url, cancellationToken);
        response.EnsureSuccessStatusCode();

        var html = await response.Content.ReadAsStringAsync(cancellationToken);

        using var initialData = YouTubeSearchParser.ExtractInitialData(html);
        if (initialData is null)
            return new VideoFeedPage(Array.Empty<VideoFeedResult>(), null);

        var items = YouTubeSearchParser.CollectVideos(initialData.RootElement)
            .Where(video => video.HasClosedCaptions)
            .Take(pageSize)
            .ToList();

        var innertube = YouTubeSearchParser.ExtractInnertube(html);
        var token = YouTubeSearchParser.FindContinuationToken(initialData.RootElement);
        var next = innertube is null || token is null
            ? null
            : new LocalContinuation(innertube.Value.ApiKey, innertube.Value.ClientVersion, token).Encode();

        return new VideoFeedPage(items, next);
    }

    private async Task<VideoFeedPage> NextPageAsync(
        LocalContinuation state, int pageSize, CancellationToken cancellationToken)
    {
        var url = $"https://www.youtube.com/youtubei/v1/search?key={Uri.EscapeDataString(state.ApiKey)}";
        var body = new
        {
            context = new { client = new { clientName = "WEB", clientVersion = state.ClientVersion, hl = "en", gl = "US" } },
            continuation = state.Token,
        };

        using var response = await _http.PostAsJsonAsync(url, body, cancellationToken);
        response.EnsureSuccessStatusCode();

        using var payload = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(cancellationToken), cancellationToken: cancellationToken);

        var items = YouTubeSearchParser.CollectVideos(payload.RootElement)
            .Where(video => video.HasClosedCaptions)
            .Take(pageSize)
            .ToList();

        var token = YouTubeSearchParser.FindContinuationToken(payload.RootElement);
        var next = token is null
            ? null
            : state with { Token = token };

        return new VideoFeedPage(items, next?.Encode());
    }

    /// <summary>
    /// The local source's continuation state - api key, client version and the youtubei token -
    /// packed into one opaque string so the query layer round-trips it without understanding it.
    /// </summary>
    private sealed record LocalContinuation(string ApiKey, string ClientVersion, string Token)
    {
        public string Encode() =>
            Convert.ToBase64String(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(this)));

        public static LocalContinuation? TryDecode(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
                return null;

            try
            {
                var json = Encoding.UTF8.GetString(Convert.FromBase64String(value));
                var decoded = JsonSerializer.Deserialize<LocalContinuation>(json);
                return string.IsNullOrWhiteSpace(decoded?.Token) ? null : decoded;
            }
            catch (Exception ex) when (ex is FormatException or JsonException or DecoderFallbackException)
            {
                return null;
            }
        }
    }
}
