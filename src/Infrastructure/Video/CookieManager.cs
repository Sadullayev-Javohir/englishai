using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// Default <see cref="ICookieManager"/>. Pools a single configured cookie file and/or every
/// <c>*.txt</c> in a configured directory, judges each file's freshness from its Netscape cookie
/// expiries, and serves the freshest usable one - re-reading the pool on every call so a file replaced
/// on disk is picked up without a restart. <see cref="Rotate"/> skips a file YouTube just rejected and
/// moves to the next; a skip is keyed by the file's last-write time, so replacing the file clears the
/// skip automatically. Thread-safe (a single lock guards the small rotation state). Never throws - an
/// unreadable file is simply treated as unusable (docs/development-guide.md rule 8).
/// </summary>
public sealed class CookieManager : ICookieManager
{
    // Cookie names whose expiry tells us a YouTube session is still alive. Any of these dated in the
    // future means the file is worth presenting to yt-dlp.
    private static readonly string[] AuthCookieNames =
    {
        "SID", "HSID", "SSID", "SAPISID", "APISID",
        "__Secure-1PSID", "__Secure-3PSID", "LOGIN_INFO",
    };

    private readonly CookieOptions _options;
    private readonly TimeProvider _clock;
    private readonly ILogger<CookieManager> _logger;
    private readonly object _gate = new();

    // Files rotated away from this process, keyed by path -> the last-write ticks they were rejected at.
    // Replacing the file on disk changes its write time, which clears the skip so the fresh file is reused.
    private readonly Dictionary<string, long> _skipped = new(StringComparer.Ordinal);

    public CookieManager(CookieOptions options, TimeProvider clock, ILogger<CookieManager> logger)
    {
        _options = options;
        _clock = clock;
        _logger = logger;
    }

    public string? GetActiveCookieFile()
    {
        lock (_gate)
            return SelectActive()?.Path;
    }

    public bool Rotate()
    {
        lock (_gate)
        {
            var current = SelectActive();
            if (current is not null)
            {
                _skipped[current.Path] = current.LastWriteTicks;
                _logger.LogWarning(
                    "YouTube cookie file {Path} was rejected/exhausted; rotating to the next pooled file.",
                    current.Path);
            }

            var next = SelectActive();
            if (next is not null)
                return true;

            _logger.LogWarning(
                "No usable YouTube cookie file remains in the pool - replace the mounted cookies. " +
                "Cookie-backed transcript fetches will be skipped until then.");
            return false;
        }
    }

    public CookieStatus GetStatus()
    {
        lock (_gate)
        {
            if (!_options.IsConfigured)
                return CookieStatus.NotConfigured;

            var files = Discover();
            var usable = files.Where(IsUsable).ToList();
            var active = SelectActiveFrom(usable);
            return new CookieStatus(
                Configured: true,
                TotalFiles: files.Count,
                UsableFiles: usable.Count,
                ActiveFile: active?.Path,
                ActiveExpiresAtUtc: active?.ExpiresAtUtc);
        }
    }

    /// <summary>Picks the freshest currently-usable file (latest expiry first), honouring rotation skips.</summary>
    private CookieFile? SelectActive() => SelectActiveFrom(Discover().Where(IsUsable).ToList());

    private static CookieFile? SelectActiveFrom(IReadOnlyList<CookieFile> usable) =>
        usable
            // Dated files first, freshest expiry first; undated (session) files last but still usable.
            .OrderByDescending(f => f.ExpiresAtUtc ?? DateTimeOffset.MinValue)
            .ThenBy(f => f.Path, StringComparer.Ordinal)
            .FirstOrDefault();

    private bool IsUsable(CookieFile file)
    {
        if (_skipped.TryGetValue(file.Path, out var skippedAt) && skippedAt == file.LastWriteTicks)
            return false;
        // A file with no dated auth cookies (session-only / unknown) is still presented - we cannot
        // disprove it; only a file whose dated cookies have all expired is treated as stale.
        return file.ExpiresAtUtc is null || file.ExpiresAtUtc > _clock.GetUtcNow();
    }

    /// <summary>Enumerates the configured single file plus every <c>*.txt</c> in the configured directory.</summary>
    private IReadOnlyList<CookieFile> Discover()
    {
        var paths = new List<string>();
        if (!string.IsNullOrWhiteSpace(_options.Path))
            paths.Add(_options.Path);

        if (!string.IsNullOrWhiteSpace(_options.Directory) && Directory.Exists(_options.Directory))
        {
            try
            {
                paths.AddRange(Directory.GetFiles(_options.Directory, "*.txt"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not list cookie directory {Dir}.", _options.Directory);
            }
        }

        var files = new List<CookieFile>();
        foreach (var path in paths.Distinct(StringComparer.Ordinal))
        {
            var file = TryRead(path);
            if (file is not null)
                files.Add(file);
        }
        return files;
    }

    private CookieFile? TryRead(string path)
    {
        try
        {
            if (!File.Exists(path))
                return null;
            var info = new FileInfo(path);
            if (info.Length == 0)
                return null;
            var expiry = LatestAuthExpiry(File.ReadAllText(path));
            return new CookieFile(path, info.LastWriteTimeUtc.Ticks, expiry);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Could not read cookie file {Path}; treating it as unusable.", path);
            return null;
        }
    }

    /// <summary>
    /// The latest expiry among the YouTube auth cookies in a Netscape cookies file, or <c>null</c> when
    /// none are dated (session-only / unrecognised). Pure and static so it is unit-testable against
    /// fixture text. The Netscape format is 7 tab-separated fields:
    /// <c>domain  flag  path  secure  expiry  name  value</c> (a leading <c>#HttpOnly_</c> on the domain
    /// is still a cookie line; other <c>#</c> lines are comments).
    /// </summary>
    internal static DateTimeOffset? LatestAuthExpiry(string content)
    {
        DateTimeOffset? latest = null;
        foreach (var raw in content.Split('\n'))
        {
            var line = raw.TrimEnd('\r');
            if (line.Length == 0)
                continue;
            if (line.StartsWith('#') && !line.StartsWith("#HttpOnly_", StringComparison.Ordinal))
                continue;

            var parts = line.Split('\t');
            if (parts.Length < 7)
                continue;
            if (!long.TryParse(parts[4], out var epoch) || epoch <= 0)
                continue; // session cookie (0) or non-numeric - carries no usable date

            var name = parts[5];
            if (!AuthCookieNames.Contains(name, StringComparer.Ordinal))
                continue;

            var expiry = DateTimeOffset.FromUnixTimeSeconds(epoch);
            if (latest is null || expiry > latest)
                latest = expiry;
        }
        return latest;
    }

    private sealed record CookieFile(string Path, long LastWriteTicks, DateTimeOffset? ExpiresAtUtc);
}
