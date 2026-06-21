using Application.Video.Models;
using FluentAssertions;
using Infrastructure.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the yt-dlp transcript provider's pure, on-disk parts without invoking the real tool
/// or the network: picking the best English json3 track yt-dlp wrote, parsing it, and staying
/// empty (honest "pending") when the tool is missing or no file parsed.
/// </summary>
public class YtDlpTranscriptProviderTests : IDisposable
{
    private const string VideoId = "abc123";

    private const string EnJson3 = """
        {"events":[
          {"tStartMs":0,"dDurationMs":2000,"segs":[{"utf8":"Hello"},{"utf8":" world."}]},
          {"tStartMs":2000,"dDurationMs":2500,"segs":[{"utf8":"Second line."}]}
        ]}
        """;

    private const string EnOrigJson3 = """
        {"events":[
          {"tStartMs":0,"dDurationMs":1000,"segs":[{"utf8":"Original only."}]}
        ]}
        """;

    private readonly string _dir = Path.Combine(Path.GetTempPath(), "ytdlp-test-" + Guid.NewGuid().ToString("N"));

    public YtDlpTranscriptProviderTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    [Fact]
    public void Parses_the_preferred_english_track_when_several_exist()
    {
        File.WriteAllText(Path.Combine(_dir, $"{VideoId}.en-orig.json3"), EnOrigJson3);
        File.WriteAllText(Path.Combine(_dir, $"{VideoId}.en.json3"), EnJson3);

        var lines = YtDlpTranscriptProvider.ParseBestFromDirectory(_dir, VideoId);

        // "en" outranks "en-orig", so the two-line manual track wins.
        lines.Should().HaveCount(2);
        lines[0].EnglishText.Should().Be("Hello world.");
        lines[1].EnglishText.Should().Be("Second line.");
    }

    [Fact]
    public void Falls_back_to_another_track_when_the_preferred_file_is_garbage()
    {
        File.WriteAllText(Path.Combine(_dir, $"{VideoId}.en.json3"), "not json at all");
        File.WriteAllText(Path.Combine(_dir, $"{VideoId}.en-orig.json3"), EnOrigJson3);

        var lines = YtDlpTranscriptProvider.ParseBestFromDirectory(_dir, VideoId);

        lines.Should().HaveCount(1);
        lines[0].EnglishText.Should().Be("Original only.");
    }

    [Fact]
    public void Returns_empty_when_no_json3_files_were_written()
    {
        var lines = YtDlpTranscriptProvider.ParseBestFromDirectory(_dir, VideoId);

        lines.Should().BeEmpty();
    }

    [Fact]
    public void Build_arguments_omits_impersonate_and_cookies_when_unset()
    {
        var args = YtDlpTranscriptProvider.BuildArguments(
            VideoId, _dir, new YtDlpOptions(), cookiePath: null, NullLogger<YtDlpTranscriptProvider>.Instance);

        args.Should().Contain("--write-auto-sub").And.Contain("json3");
        args.Should().NotContain("--impersonate");
        args.Should().NotContain("--cookies");
        args[^1].Should().Be("https://www.youtube.com/watch?v=" + VideoId);
    }

    [Fact]
    public void Build_arguments_adds_player_client_bypass_by_default()
    {
        // The default options carry the cookie-less bot-wall bypass, so production gets it with no
        // configuration: --extractor-args "youtube:player_client=web_safari,default".
        var args = YtDlpTranscriptProvider.BuildArguments(
            VideoId, _dir, new YtDlpOptions(), cookiePath: null, NullLogger<YtDlpTranscriptProvider>.Instance);

        args.Should().ContainInOrder("--extractor-args", "youtube:player_client=web_safari,default");
    }

    [Fact]
    public void Build_arguments_omits_player_client_when_explicitly_disabled()
    {
        var args = YtDlpTranscriptProvider.BuildArguments(
            VideoId, _dir, new YtDlpOptions { PlayerClient = "" }, cookiePath: null,
            NullLogger<YtDlpTranscriptProvider>.Instance);

        args.Should().NotContain("--extractor-args");
    }

    [Fact]
    public void Build_arguments_adds_impersonate_when_configured()
    {
        var args = YtDlpTranscriptProvider.BuildArguments(
            VideoId, _dir, new YtDlpOptions { Impersonate = "chrome" }, cookiePath: null,
            NullLogger<YtDlpTranscriptProvider>.Instance);

        args.Should().ContainInOrder("--impersonate", "chrome");
    }

    [Fact]
    public void Build_arguments_adds_cookies_only_when_the_file_exists()
    {
        var cookiesFile = Path.Combine(_dir, "cookies.txt");
        File.WriteAllText(cookiesFile, "# Netscape HTTP Cookie File\n");

        // The cookie path now comes from the central CookieManager (passed in), not the options.
        var withCookies = YtDlpTranscriptProvider.BuildArguments(
            VideoId, _dir, new YtDlpOptions(), cookiePath: cookiesFile, NullLogger<YtDlpTranscriptProvider>.Instance);
        withCookies.Should().ContainInOrder("--cookies", cookiesFile);

        // A selected-but-missing cookies file must not become a --cookies flag (yt-dlp would error);
        // it is logged and the fetch proceeds cookie-less instead.
        var missing = YtDlpTranscriptProvider.BuildArguments(
            VideoId, _dir, new YtDlpOptions(), cookiePath: Path.Combine(_dir, "nope.txt"),
            NullLogger<YtDlpTranscriptProvider>.Instance);
        missing.Should().NotContain("--cookies");
    }

    [Fact]
    public async Task Fetch_reports_provider_unavailable_when_the_tool_is_missing()
    {
        // A configured-but-nonexistent path resolves to no executable → ProviderUnavailable (not
        // NoCaptions), so the fill stays Pending to retry rather than being made terminal. Never throws.
        var options = new YtDlpOptions { Path = "/nonexistent/yt-dlp-binary" };
        var provider = new YtDlpTranscriptProvider(options, NullLogger<YtDlpTranscriptProvider>.Instance);

        var result = await provider.FetchAsync(VideoId, CancellationToken.None);

        result.Outcome.Should().Be(TranscriptFetchOutcome.ProviderUnavailable);
        result.Lines.Should().BeEmpty();
    }
}
