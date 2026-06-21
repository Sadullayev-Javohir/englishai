using FluentAssertions;
using Infrastructure.Video;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Integration.Tests.Video;

/// <summary>
/// Verifies the central <see cref="CookieManager"/>: it picks the freshest usable cookie file, treats a
/// file whose YouTube auth cookies have all expired as stale, rotates past a rejected file to the next,
/// and reports an honest status - all without persisting anything (docs/development-guide.md rule 8).
/// </summary>
public class CookieManagerTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), "cookies-test-" + Guid.NewGuid().ToString("N"));
    private readonly FixedClock _clock = new(DateTimeOffset.Parse("2026-06-28T00:00:00Z"));

    public CookieManagerTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); } catch { /* best-effort */ }
    }

    private CookieManager Build(CookieOptions options) =>
        new(options, _clock, NullLogger<CookieManager>.Instance);

    private static string CookieLine(string name, DateTimeOffset expiry) =>
        $".youtube.com\tTRUE\t/\tTRUE\t{expiry.ToUnixTimeSeconds()}\t{name}\tvalue";

    private string WriteCookie(string fileName, params string[] lines)
    {
        var path = Path.Combine(_dir, fileName);
        File.WriteAllText(path, "# Netscape HTTP Cookie File\n" + string.Join('\n', lines) + "\n");
        return path;
    }

    [Fact]
    public void Latest_auth_expiry_reads_the_freshest_dated_youtube_cookie()
    {
        var content =
            "# Netscape HTTP Cookie File\n" +
            CookieLine("SID", DateTimeOffset.Parse("2026-08-01T00:00:00Z")) + "\n" +
            CookieLine("SAPISID", DateTimeOffset.Parse("2026-09-01T00:00:00Z")) + "\n" +
            ".youtube.com\tTRUE\t/\tTRUE\t0\tSESSION_ONLY\tx\n"; // session cookie ignored

        var expiry = CookieManager.LatestAuthExpiry(content);

        expiry.Should().Be(DateTimeOffset.Parse("2026-09-01T00:00:00Z"));
    }

    [Fact]
    public void Not_configured_status_when_no_cookie_source_is_set()
    {
        var manager = Build(new CookieOptions());

        manager.GetActiveCookieFile().Should().BeNull();
        manager.GetStatus().Configured.Should().BeFalse();
    }

    [Fact]
    public void Picks_the_freshest_usable_file_and_skips_the_expired_one()
    {
        WriteCookie("expired.txt", CookieLine("SID", _clock.GetUtcNow().AddDays(-1)));
        var fresh = WriteCookie("fresh.txt", CookieLine("SID", _clock.GetUtcNow().AddDays(30)));

        var manager = Build(new CookieOptions { Directory = _dir });

        manager.GetActiveCookieFile().Should().Be(fresh);
        var status = manager.GetStatus();
        status.TotalFiles.Should().Be(2);
        status.UsableFiles.Should().Be(1);
    }

    [Fact]
    public void Rotate_moves_to_the_next_usable_file_then_reports_none_left()
    {
        var a = WriteCookie("a.txt", CookieLine("SID", _clock.GetUtcNow().AddDays(40)));
        var b = WriteCookie("b.txt", CookieLine("SID", _clock.GetUtcNow().AddDays(30)));

        var manager = Build(new CookieOptions { Directory = _dir });

        // Freshest first (a, +40d).
        manager.GetActiveCookieFile().Should().Be(a);

        // Reject a -> b becomes active.
        manager.Rotate().Should().BeTrue();
        manager.GetActiveCookieFile().Should().Be(b);

        // Reject b -> none left.
        manager.Rotate().Should().BeFalse();
        manager.GetActiveCookieFile().Should().BeNull();
        manager.GetStatus().NeedsRefresh.Should().BeTrue();
    }

    [Fact]
    public void All_expired_pool_needs_refresh()
    {
        WriteCookie("old.txt", CookieLine("SID", _clock.GetUtcNow().AddDays(-5)));

        var manager = Build(new CookieOptions { Directory = _dir });

        manager.GetActiveCookieFile().Should().BeNull();
        manager.GetStatus().NeedsRefresh.Should().BeTrue();
    }

    private sealed class FixedClock : TimeProvider
    {
        private readonly DateTimeOffset _now;
        public FixedClock(DateTimeOffset now) => _now = now;
        public override DateTimeOffset GetUtcNow() => _now;
    }
}
