using System.Diagnostics;
using System.Text.Json;
using Application.Video.Models;
using Application.Video.Ports;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Video;

/// <summary>
/// <see cref="IVideoTranscriptProvider"/> backed by the <c>yt-dlp</c> tool (PROJECT-SPEC B.3,
/// Bosqich 3). It writes the video's English caption track (manual or auto-generated) as
/// <c>timedtext</c> <c>fmt=json3</c> to a temp directory, then parses it with
/// <see cref="TranscriptParser.ParseTimedTextJson3"/>.
///
/// WHY yt-dlp: YouTube's direct <c>timedtext</c>/<c>get_transcript</c> endpoints return an
/// empty body (HTTP 200, 0 bytes) for datacenter/sandbox and many residential IPs - a hard,
/// verified block that the plain-HTTP provider cannot defeat. <c>yt-dlp</c> negotiates the
/// caption surface the way the YouTube client does and reliably returns captions for any video
/// that has them. Any failure (tool missing, no captions, network) yields an empty list, never
/// an exception, so the lesson keeps its honest "transcript pending" state (docs/development-guide.md rules 8, 11).
/// </summary>
public sealed class YtDlpTranscriptProvider : IVideoTranscriptProvider
{
    // Specific English variants only (never a glob): a broad "en.*" match makes yt-dlp request
    // dozens of "X from English" auto-translations and trips YouTube's HTTP 429 rate limit.
    private const string SubLangs = "en-orig,en,en-US,en-GB";

    // The order we prefer when several English json3 files were written, by the lang suffix
    // yt-dlp appends to the filename ("<id>.<lang>.json3").
    private static readonly string[] LangPriority = { "en", "en-US", "en-GB", "en-orig", "en-en" };

    private readonly YtDlpOptions _options;
    private readonly ICookieManager? _cookies;
    private readonly ILogger<YtDlpTranscriptProvider> _logger;

    /// <summary>
    /// Creates a yt-dlp provider. Pass an <paramref name="cookies"/> manager to make this the
    /// cookie-backed attempt (it resolves the freshest pooled cookie file per fetch); pass <c>null</c>
    /// for the cookie-less attempt. This lets the orchestrator register both - a free cookie-less try
    /// and a cookie-backed try - from the same class (docs/development-guide.md rule 8).
    /// </summary>
    public YtDlpTranscriptProvider(
        YtDlpOptions options, ILogger<YtDlpTranscriptProvider> logger, ICookieManager? cookies = null)
    {
        _options = options;
        _cookies = cookies;
        _logger = logger;
    }

    public async Task<TranscriptFetchResult> FetchAsync(
        string youTubeVideoId, CancellationToken cancellationToken = default)
    {
        var executable = YtDlpExecutable.Resolve(_options.Path);
        if (executable is null)
        {
            _logger.LogWarning(
                "yt-dlp not found (configured path: {Path}); transcript stays pending for retry. " +
                "Install it to tools/yt-dlp or set YtDlp:Path.", _options.Path ?? "<none>");
            return TranscriptFetchResult.ProviderUnavailable;
        }

        // The cookie file is resolved per fetch from the central manager (freshest pooled file), so a
        // rotated/replaced cookie is picked up without restarting; null = the cookie-less attempt.
        var cookiePath = _cookies?.GetActiveCookieFile() ?? _options.CookiesPath;

        var workDir = Path.Combine(Path.GetTempPath(), "englishai-yt", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDir);
        try
        {
            var exitedCleanly = await RunAsync(executable, youTubeVideoId, workDir, cookiePath, cancellationToken);
            var lines = ParseBestFromDirectory(workDir, youTubeVideoId);
            if (lines.Count > 0)
                return TranscriptFetchResult.Fetched(lines);

            // No lines parsed. A clean exit (code 0) means yt-dlp ran and the video simply has no
            // English caption track - terminal. A non-zero exit means yt-dlp itself failed
            // (rate-limit, bot-check, network) - transient, so the fill is retried later (rule 8).
            return exitedCleanly
                ? TranscriptFetchResult.NoCaptions
                : TranscriptFetchResult.ProviderUnavailable;
        }
        catch (Exception ex)
        {
            // The process could not start (e.g. python3 missing), timed out, or threw - never
            // terminal: leave the lesson pending so a later open retries (docs/development-guide.md rule 8).
            _logger.LogWarning(ex, "yt-dlp transcript fetch failed for video {VideoId}; will retry.", youTubeVideoId);
            return TranscriptFetchResult.ProviderUnavailable;
        }
        finally
        {
            TryDeleteDirectory(workDir);
        }
    }

    /// <summary>
    /// Runs yt-dlp into <paramref name="workDir"/>; returns <c>true</c> when it exited cleanly
    /// (code 0), <c>false</c> on a non-zero exit (which we treat as a transient/blocked failure).
    /// </summary>
    private async Task<bool> RunAsync(
        string executable, string videoId, string workDir, string? cookiePath, CancellationToken cancellationToken)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            WorkingDirectory = workDir,
        };
        foreach (var arg in BuildArguments(videoId, workDir, _options, cookiePath, _logger))
            psi.ArgumentList.Add(arg);

        using var process = new Process { StartInfo = psi };
        process.Start();

        // Drain the pipes so a chatty process never deadlocks on a full buffer.
        var stdout = process.StandardOutput.ReadToEndAsync(cancellationToken);
        var stderr = process.StandardError.ReadToEndAsync(cancellationToken);

        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromSeconds(Math.Max(5, _options.TimeoutSeconds)));
        try
        {
            await process.WaitForExitAsync(timeout.Token);
        }
        catch (OperationCanceledException)
        {
            TryKill(process);
            throw;
        }

        if (process.ExitCode != 0)
        {
            _logger.LogWarning(
                "yt-dlp exited with code {Code} for video {VideoId}: {Error}",
                process.ExitCode, videoId, (await stderr).Trim());
            return false;
        }

        return true;
    }

    /// <summary>
    /// Builds the yt-dlp command line for a caption fetch into <paramref name="workDir"/>. Static and
    /// option-driven so it is unit-testable: the impersonation and cookies flags (the two levers that
    /// get a datacenter/VPS IP past YouTube's bot challenge) are added only when configured. A
    /// <c>CookiesPath</c> that points at a missing file is logged and skipped rather than failing the
    /// fetch, so local dev without cookies still works (docs/development-guide.md rule 8).
    /// </summary>
    internal static IReadOnlyList<string> BuildArguments(
        string videoId, string workDir, YtDlpOptions options, string? cookiePath, ILogger logger)
    {
        var args = new List<string>
        {
            "--skip-download",
            "--write-sub",
            "--write-auto-sub",
            "--sub-langs", SubLangs,
            "--sub-format", "json3",
            "--no-playlist",
            "--no-warnings",
            "--no-progress",
        };

        // Select the YouTube player clients used to negotiate the caption surface. This is the FREE,
        // cookie-less bot-wall bypass: on a datacenter/VPS IP the default web client gets "Sign in to
        // confirm you're not a bot" and reports every video unavailable, but the web_safari/innertube
        // clients take a different bot-check path that still returns captions. Defaults ON (see
        // YtDlpOptions.PlayerClient); an empty value disables the flag (docs/development-guide.md rule 8).
        if (!string.IsNullOrWhiteSpace(options.PlayerClient))
        {
            args.Add("--extractor-args");
            args.Add("youtube:player_client=" + options.PlayerClient);
        }

        // Impersonate a real browser's TLS/HTTP fingerprint when configured. This is what lets a
        // server/datacenter IP fetch captions at all - without it YouTube serves the bot challenge
        // and yt-dlp reports every video as "unavailable", so the lesson transcript never fills
        // (requires curl_cffi in the runtime image; left unset in local dev). docs/development-guide.md rule 8.
        if (!string.IsNullOrWhiteSpace(options.Impersonate))
        {
            args.Add("--impersonate");
            args.Add(options.Impersonate);
        }

        // Real account cookies are the most reliable way past YouTube's datacenter bot challenge - when
        // impersonation alone is not enough (the common case on a VPS), these defeat the "confirm you're
        // not a bot" wall. Only used when the configured file actually exists; a missing file is logged
        // and the fetch proceeds cookie-less (so a half-configured prod or a cookie-less dev still runs).
        if (!string.IsNullOrWhiteSpace(cookiePath))
        {
            if (File.Exists(cookiePath))
            {
                args.Add("--cookies");
                args.Add(cookiePath);
            }
            else
            {
                logger.LogWarning(
                    "Cookie file {Path} was selected but no file exists there; fetching without cookies. " +
                    "On a datacenter IP YouTube may block this - mount the exported cookies file at that path.",
                    cookiePath);
            }
        }

        args.Add("-o");
        args.Add(Path.Combine(workDir, "%(id)s.%(ext)s"));
        args.Add("https://www.youtube.com/watch?v=" + videoId);
        return args;
    }

    /// <summary>
    /// Picks the best English json3 caption file yt-dlp wrote into <paramref name="workDir"/>
    /// and parses it; returns empty when none parsed to a non-empty transcript. Static and
    /// I/O-only-from-disk so it is unit-testable against fixture files.
    /// </summary>
    internal static IReadOnlyList<TranscriptLine> ParseBestFromDirectory(string workDir, string videoId)
    {
        if (!Directory.Exists(workDir))
            return Array.Empty<TranscriptLine>();

        var files = Directory.GetFiles(workDir, "*.json3");
        foreach (var file in OrderByPreference(files, videoId))
        {
            try
            {
                using var doc = JsonDocument.Parse(File.ReadAllText(file));
                var lines = TranscriptParser.ParseTimedTextJson3(doc.RootElement);
                if (lines.Count > 0)
                    return lines;
            }
            catch (JsonException)
            {
                // A truncated/garbage file is skipped; another track may still parse.
            }
        }

        return Array.Empty<TranscriptLine>();
    }

    /// <summary>Orders json3 files by our English-variant preference, then by name for determinism.</summary>
    private static IEnumerable<string> OrderByPreference(IEnumerable<string> files, string videoId)
    {
        var prefix = videoId + ".";
        return files
            .OrderBy(f => PriorityOf(LangOf(Path.GetFileName(f), prefix)))
            .ThenBy(f => Path.GetFileName(f), StringComparer.Ordinal);
    }

    /// <summary>The lang suffix from "&lt;id&gt;.&lt;lang&gt;.json3" (or the whole stem if it has no id prefix).</summary>
    private static string LangOf(string fileName, string prefix)
    {
        var stem = fileName.EndsWith(".json3", StringComparison.OrdinalIgnoreCase)
            ? fileName[..^".json3".Length]
            : fileName;
        if (stem.StartsWith(prefix, StringComparison.Ordinal))
            stem = stem[prefix.Length..];
        return stem;
    }

    private static int PriorityOf(string lang)
    {
        var index = Array.IndexOf(LangPriority, lang);
        return index >= 0 ? index : LangPriority.Length;
    }

    private static void TryKill(Process process)
    {
        try
        {
            if (!process.HasExited)
                process.Kill(entireProcessTree: true);
        }
        catch
        {
            // Best-effort cleanup.
        }
    }

    private static void TryDeleteDirectory(string dir)
    {
        try
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, recursive: true);
        }
        catch
        {
            // Temp cleanup is best-effort.
        }
    }
}
