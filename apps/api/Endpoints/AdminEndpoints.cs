using System.IdentityModel.Tokens.Jwt;
using System.Text.RegularExpressions;
using System.Security.Claims;
using Application.Admin.Dtos;
using Application.Admin.Ports;
using Application.Admin.ClearRecentLogs;
using Application.Admin.DeleteRecentLog;
using Application.Admin.ForceDueReviews;
using Application.Admin.GetAdminAccess;
using Application.Admin.GetAdminUserDetail;
using Application.Admin.GetAdminUsers;
using Application.Admin.SetUserAdmin;
using Application.Admin.SetVariableCostBudget;
using Application.Admin.GetServerDiagnostics;
using Application.Analytics.GetFounderMetrics;
using Application.Common;
using Application.Notifications.DeleteAdminBroadcast;
using Application.Notifications.GetAdminBroadcasts;
using Application.Notifications.SendAdminBroadcast;
using Application.Notifications.TriggerDailyDispatch;
using Application.Grammar.Admin;
using Application.Grammar.Admin.CreateGrammarLesson;
using Application.Grammar.Admin.DeleteGrammarLesson;
using Application.Grammar.Admin.GetAllGrammarLessons;
using Application.Grammar.Admin.GetGrammarLesson;
using Application.Grammar.Admin.UpdateGrammarLesson;
using Application.Grammar.Admin.UpdateGrammarLessonContent;
using Application.Listening.Admin;
using Application.Listening.Admin.CreateListeningExercise;
using Application.Listening.Admin.DeleteListeningExercise;
using Application.Listening.Admin.GetAllListeningExercises;
using Application.Listening.Admin.GetListeningExercise;
using Application.Listening.Admin.UpdateListeningExercise;
using Application.Listening.Admin.UpdateListeningExerciseContent;
using Application.Reading.Admin;
using Application.Reading.Admin.CreateReadingPassage;
using Application.Reading.Admin.DeleteReadingPassage;
using Application.Reading.Admin.GetAllReadingPassages;
using Application.Reading.Admin.GetReadingPassageAdmin;
using Application.Reading.Admin.UpdateReadingPassage;
using Application.Reading.Admin.UpdateReadingPassageFull;
using Application.Vocabulary.Admin.CreateVocabularyTopic;
using Application.Vocabulary.Admin.DeleteVocabularyTopic;
using Application.Vocabulary.Admin.GetAllVocabularyTopics;
using Application.Vocabulary.Admin.GetVocabularyTopic;
using Application.Vocabulary.Admin.UpdateVocabularyTopic;
using Application.Vocabulary.Admin.Images;
using Application.Writing.Admin;
using Application.Writing.Admin.CreateWritingTask;
using Application.Writing.Admin.DeleteWritingTask;
using Application.Writing.Admin.GetAllWritingTasks;
using Application.Writing.Admin.UpdateWritingTask;
using Application.Video.Models;
using Application.Video.Ports;
using Application.Books.Ports;
using Application.Books.Dtos;
using Domain.Books;
using Domain.Curriculum;
using Domain.Assessment;
using Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Infrastructure.Jobs;
using Infrastructure.Backfill;
using Infrastructure.Images;
using Infrastructure.Llm;
using Infrastructure.Video;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace Web.Endpoints;

/// <summary>
/// Operator-only HTTP surface. The content backfill eagerly generates and caches every learning
/// module's content for every topic via the Anthropic Batch API (docs/development-guide.md rule 10), so users are
/// always served from the database and the LLM is never on the request path. Because a run incurs
/// real (one-time) LLM cost it is guarded by an admin token (set in user-secrets / env per rule 13,
/// never in code) and runs off the request path on Hangfire.
/// </summary>
public static class AdminEndpoints
{
    private const string AdminTokenHeader = "X-Admin-Token";
    private const string AdminTokenConfigKey = "Backfill:AdminToken";
    private const string OperationsTokenHeader = "X-Operations-Token";
    private const string OperationsTokenConfigKey = "Operations:BotToken";
    private static readonly Regex SensitiveValuePattern = new(
        "(?i)(token|password|secret|authorization|api[-_ ]?key|cookie)\\s*[:=]\\s*[^\\s,;]+",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly Regex EmailPattern = new(
        "(?i)\\b[A-Z0-9._%+-]+@[A-Z0-9.-]+\\.[A-Z]{2,}\\b",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static IEndpointRouteBuilder MapAdminEndpoints(this IEndpointRouteBuilder app)
    {
        // These operator endpoints carry their own admin-token guard (X-Admin-Token), so they opt out
        // of the global JWT fallback policy - the token is their authentication, not a user cookie.
        var group = app.MapGroup("/api/admin").WithTags("Admin").AllowAnonymous();

        app.MapGet("/api/operations/server-diagnostics", async (
            HttpContext http,
            IConfiguration config,
            IServerDiagnosticsProvider diagnostics,
            CancellationToken cancellationToken) =>
        {
            var expected = config[OperationsTokenConfigKey];
            var supplied = http.Request.Headers[OperationsTokenHeader].ToString();
            if (string.IsNullOrWhiteSpace(expected) ||
                !Web.Observability.MetricsProtectionMiddleware.IsPrivate(http.Connection.RemoteIpAddress) ||
                !Web.Observability.MetricsProtectionMiddleware.FixedTimeEquals(expected, supplied))
                return Results.NotFound();

            var snapshot = await diagnostics.CollectAsync(cancellationToken);
            var recent = snapshot.Logs.Recent
                .Take(50)
                .Select(entry => new OperationsLogEntryDto(
                    entry.Id,
                    entry.TimestampUtc,
                    entry.Level,
                    SanitizeOperationsMessage(entry.Message)))
                .ToArray();

            return Results.Ok(new OperationsDiagnosticsDto(
                snapshot.GeneratedAtUtc,
                snapshot.OverallStatus,
                snapshot.Dependencies,
                snapshot.Logs.WarningCount,
                snapshot.Logs.ErrorCount,
                recent));
        }).AllowAnonymous().ExcludeFromDescription();

        // Enqueues a one-time, idempotent content backfill (re-runnable to fill only the gaps).
        group.MapPost("/backfill", (HttpContext http, IConfiguration config, IBackgroundJobScheduler? jobs) =>
        {
            if (!http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided) || string.IsNullOrEmpty(provided))
                return Results.Unauthorized();

            var expected = config[AdminTokenConfigKey];
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem(
                    "Backfill is disabled: no admin token configured (set Backfill:AdminToken).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
                return Results.Unauthorized();

            if (jobs is null)
                return Results.Problem(
                    "Backfill requires Hangfire (a background runner) to be enabled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var jobId = jobs.EnqueueContent<ContentBackfillJob>(job => job.RunAsync(CancellationToken.None));
            return Results.Accepted($"/api/admin/backfill/{jobId}", new { jobId });
        });

        group.MapPost("/backfill-topic-vocabulary", (
            HttpContext http, IConfiguration config, IBackgroundJobScheduler? jobs) =>
        {
            if (!http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided) || string.IsNullOrEmpty(provided))
                return Results.Unauthorized();

            var expected = config[AdminTokenConfigKey];
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem(
                    "Topic vocabulary backfill is disabled: no admin token configured (set Backfill:AdminToken).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
                return Results.Unauthorized();

            if (jobs is null)
                return Results.Problem(
                    "Topic vocabulary backfill requires Hangfire (a background runner) to be enabled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var jobId = jobs.EnqueueContent<TopicVocabularyBackfillJob>(
                job => job.RunAsync(CancellationToken.None));
            return Results.Accepted($"/api/admin/backfill-topic-vocabulary/{jobId}", new { jobId });
        });

        // Live diagnostic for the primary internal Hermes Agent Gateway. Token-guarded like the
        // backfill because it makes one real chat-completions call.
        group.MapGet("/content-health", async (
            HttpContext http, IConfiguration config, HermesGatewayOptions hermes,
            HermesGatewayLlmCompletion llm, CancellationToken cancellationToken) =>
        {
            if (!http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided) || string.IsNullOrEmpty(provided))
                return Results.Unauthorized();

            var expected = config[AdminTokenConfigKey];
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem(
                    "Content health is disabled: no admin token configured (set Backfill:AdminToken).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
                return Results.Unauthorized();

            if (!hermes.IsConfigured)
                return Results.Ok(new
                {
                    hermesConfigured = false,
                    hermesModel = hermes.Model,
                    probe = "skipped",
                    detail = "No Hermes Gateway API key is configured. Offline fallbacks are in use.",
                });

            var reply = await llm.CompleteAsync(
                "Reply with the single word OK.", "ping", maxOutputTokens: 16, cancellationToken);

            return Results.Ok(new
            {
                hermesConfigured = true,
                hermesModel = hermes.Model,
                probe = reply is not null ? "ok" : "failed",
                detail = reply is not null
                    ? "Hermes Agent Gateway reached successfully."
                    : "Hermes Agent Gateway call failed; check its health, API key, model and application logs.",
            });
        });

        // Enqueues a one-time, idempotent topic-image backfill: downloads one licensed thumbnail per
        // topic into the database (rule 12). Re-runnable to fill only the gaps (the provider
        // rate-limits, so a topic that fails one run is retried on the next).
        group.MapPost("/backfill-images", (HttpContext http, IConfiguration config, IBackgroundJobScheduler? jobs) =>
        {
            if (!http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided) || string.IsNullOrEmpty(provided))
                return Results.Unauthorized();

            var expected = config[AdminTokenConfigKey];
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem(
                    "Image backfill is disabled: no admin token configured (set Backfill:AdminToken).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
                return Results.Unauthorized();

            if (jobs is null)
                return Results.Problem(
                    "Image backfill requires Hangfire (a background runner) to be enabled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var jobId = jobs.EnqueueContent<TopicImageBackfillJob>(job => job.RunAsync(CancellationToken.None));
            return Results.Accepted($"/api/admin/backfill-images/{jobId}", new { jobId });
        });

        // Fills every vocabulary flashcard with its own licensed, safety-checked image. The job is
        // idempotent and continues in bounded batches until all filled topics' words have images.
        group.MapPost("/backfill-word-images", (
            HttpContext http, IConfiguration config, IBackgroundJobScheduler? jobs) =>
        {
            if (!HasAdminToken(http, config))
                return Results.Unauthorized();
            if (jobs is null)
                return Results.Problem(
                    "Word image backfill requires Hangfire to be enabled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var jobId = jobs.EnqueueContent<WordImageBackfillJob>(
                job => job.RunAsync(CancellationToken.None));
            return Results.Accepted($"/api/admin/backfill-word-images/{jobId}", new { jobId });
        });

        group.MapPost("/images/audit-and-remediate", (
            HttpContext http, IConfiguration config, IBackgroundJobScheduler? jobs) =>
        {
            if (!HasAdminToken(http, config))
                return Results.Unauthorized();
            if (jobs is null)
                return Results.Problem(
                    "Image safety audit requires Hangfire to be enabled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var jobId = jobs.EnqueueContent<ImageSafetyAuditJob>(
                job => job.RunAsync(CancellationToken.None));
            return Results.Accepted($"/api/admin/images/audit-and-remediate/{jobId}", new { jobId });
        });

        group.MapGet("/images/status", async (
            HttpContext http,
            IConfiguration config,
            CancellationToken cancellationToken) =>
        {
            if (!HasAdminToken(http, config))
                return Results.Unauthorized();

            var store = http.RequestServices.GetRequiredService<ITopicImageStore>();
            var db = http.RequestServices.GetRequiredService<EnglishAiDbContext>();

            var inventory = await store.GetSafetyInventoryAsync(cancellationToken);
            var totalWords = await db.VocabularyTopics.AsNoTracking()
                .SelectMany(topic => topic.Words)
                .CountAsync(cancellationToken);
            var markedWords = await db.VocabularyTopics.AsNoTracking()
                .SelectMany(topic => topic.Words)
                .CountAsync(word => word.ImageUrl != null && word.ImageUrl != string.Empty, cancellationToken);
            return Results.Ok(new
            {
                images = inventory,
                vocabularyWords = new { total = totalWords, persistedMarkers = markedWords, missing = totalWords - markedWords },
            });
        });

        // Re-encodes every stored topic image to compact WebP IN PLACE (no re-download) so the
        // bytea gallery stops eating disk. Idempotent - already-webp / non-shrinking images are
        // left alone, so it is safe to re-run. Token-guarded like the other operator backfills.
        group.MapPost("/reencode-images", (HttpContext http, IConfiguration config, IBackgroundJobScheduler? jobs) =>
        {
            if (!http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided) || string.IsNullOrEmpty(provided))
                return Results.Unauthorized();

            var expected = config[AdminTokenConfigKey];
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem(
                    "Image re-encode is disabled: no admin token configured (set Backfill:AdminToken).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
                return Results.Unauthorized();

            if (jobs is null)
                return Results.Problem(
                    "Image re-encode requires Hangfire (a background runner) to be enabled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            var jobId = jobs.EnqueueMaintenance<TopicImageReencodeJob>(job => job.RunAsync(CancellationToken.None));
            return Results.Accepted($"/api/admin/reencode-images/{jobId}", new { jobId });
        });

        // Deletes every stored topic image, then a follow-up POST /api/admin/backfill-images
        // re-downloads a fresh, safe-filtered gallery for every topic. This is the remediation path
        // when unsuitable images slipped through (the backfill alone is idempotent and would leave an
        // already-full - but inappropriate - gallery untouched). Token-guarded like the backfill.
        group.MapPost("/purge-images", async (
            HttpContext http, IConfiguration config, ITopicImageStore store, CancellationToken cancellationToken) =>
        {
            if (!http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided) || string.IsNullOrEmpty(provided))
                return Results.Unauthorized();

            var expected = config[AdminTokenConfigKey];
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem(
                    "Image purge is disabled: no admin token configured (set Backfill:AdminToken).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
                return Results.Unauthorized();

            var removed = await store.DeleteAllAsync(cancellationToken);
            return Results.Ok(new
            {
                removed,
                next = "POST /api/admin/backfill-images to re-download a fresh, safe-filtered gallery.",
            });
        });

        // Live diagnostic: can THIS server actually fetch a video's captions? The interactive
        // transcript fills from yt-dlp, and from a datacenter/VPS IP YouTube blocks it (bot challenge),
        // so a lesson's transcript silently never loads. This runs the real provider against a video id
        // and reports the outcome, so a prod failure is visible instead of a perpetual "pending" spinner.
        // Token-guarded like the other operator probes. Pass ?videoId=<id> (defaults to a known-captioned
        // video) to test.
        group.MapGet("/transcript-health", async (
            HttpContext http, IConfiguration config, IVideoTranscriptProvider provider,
            ICookieManager cookies, string? videoId, CancellationToken cancellationToken) =>
        {
            if (!http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided) || string.IsNullOrEmpty(provided))
                return Results.Unauthorized();

            var expected = config[AdminTokenConfigKey];
            if (string.IsNullOrWhiteSpace(expected))
                return Results.Problem(
                    "Transcript health is disabled: no admin token configured (set Backfill:AdminToken).",
                    statusCode: StatusCodes.Status503ServiceUnavailable);

            if (!string.Equals(provided.ToString(), expected, StringComparison.Ordinal))
                return Results.Unauthorized();

            // Default to a stable, captioned video so a bare call still exercises the real path.
            var id = string.IsNullOrWhiteSpace(videoId) ? "dQw4w9WgXcQ" : videoId.Trim();
            var result = await provider.FetchAsync(id, cancellationToken);
            var cookieStatus = cookies.GetStatus();

            return Results.Ok(new
            {
                videoId = id,
                outcome = result.Outcome.ToString(),
                lineCount = result.Lines.Count,
                // Cookie pool health: surfaces "configured but every file is stale" so an operator knows
                // to replace the mounted cookies before the cookie-backed provider can help.
                cookies = new
                {
                    configured = cookieStatus.Configured,
                    totalFiles = cookieStatus.TotalFiles,
                    usableFiles = cookieStatus.UsableFiles,
                    needsRefresh = cookieStatus.NeedsRefresh,
                    activeExpiresAtUtc = cookieStatus.ActiveExpiresAtUtc,
                },
                detail = result.Outcome switch
                {
                    TranscriptFetchOutcome.Fetched =>
                        "Captions fetched successfully - transcripts will fill on this server.",
                    TranscriptFetchOutcome.NoCaptions =>
                        "yt-dlp ran but this specific video has no English caption track (try another videoId).",
                    _ =>
                        "yt-dlp could not fetch captions (provider unavailable). On a datacenter/VPS IP this is "
                        + "almost always YouTube's bot block - ensure curl_cffi + the YtDlp__Impersonate env var "
                        + "(and a JS runtime/deno) are present in the image; check the app log for the yt-dlp "
                        + "exit code and stderr. If it persists, the IP needs cookies or a residential proxy.",
                },
            });
        });

        MapAdminPanelEndpoints(app);
        return app;
    }

    private static bool HasAdminToken(HttpContext http, IConfiguration config) =>
        http.Request.Headers.TryGetValue(AdminTokenHeader, out var provided)
        && !string.IsNullOrWhiteSpace(provided)
        && !string.IsNullOrWhiteSpace(config[AdminTokenConfigKey])
        && string.Equals(provided.ToString(), config[AdminTokenConfigKey], StringComparison.Ordinal);

    private static string SanitizeOperationsMessage(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
            return "No message.";

        var sanitized = SensitiveValuePattern.Replace(message, "$1=[REDACTED]");
        sanitized = EmailPattern.Replace(sanitized, "[REDACTED_EMAIL]");
        sanitized = sanitized.Replace('\r', ' ').Replace('\n', ' ').Trim();
        return sanitized.Length <= 500 ? sanitized : sanitized[..500] + "…";
    }

    /// <summary>
    /// The user-facing admin panel surface (PROJECT-SPEC operator console). Unlike the operator
    /// endpoints above - which authenticate with the static <c>X-Admin-Token</c> - these identify the
    /// caller from their normal session cookie and authorize per-account: any signed-in user may read
    /// their own admin standing, an admin may list users, and only the super-admin may promote/demote.
    /// The role checks live in the MediatR handlers (they 403 on failure), so the endpoints stay thin.
    /// </summary>
    private static void MapAdminPanelEndpoints(IEndpointRouteBuilder app)
    {
        // Authenticated (the global fallback policy already requires a user); the per-role checks
        // happen in the handlers against the session identity.
        var group = app.MapGroup("/api/admin").WithTags("Admin");

        // The signed-in user's own admin standing, so the SPA can decide whether to show the panel.
        group.MapGet("/access", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAdminAccessQuery(userId.Value)));
        });

        // The full user list. The handler rejects non-admins with a 403.
        group.MapGet("/users", async (string? cursor, int? pageSize, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAdminUsersQuery(
                userId.Value, cursor, Math.Min(pageSize ?? 50, 100))));
        });

        group.MapGet("/users/{targetId:guid}", async (Guid targetId, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAdminUserDetailQuery(userId.Value, targetId)));
        });

        // The founder growth dashboard (DAU/retention/conversion). The handler rejects non-admins
        // with a 403; the acting user is taken from the session, never the request.
        group.MapGet("/metrics", async (DateOnly? from, DateOnly? to, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetFounderMetricsQuery(userId.Value, from, to)));
        });

        group.MapPut("/metrics/cost-budget", async (
            SetVariableCostBudgetRequest request,
            HttpContext http,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new SetVariableCostBudgetCommand(userId.Value, request.DailyBudgetUsd),
                cancellationToken));
        });

        group.MapGet("/metrics/stream", async (
            DateOnly? from,
            DateOnly? to,
            HttpContext http,
            ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
            {
                http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            await WriteEventStreamHeadersAsync(http);
            using var timer = new PeriodicTimer(TimeSpan.FromSeconds(15));
            do
            {
                var metrics = await sender.Send(
                    new GetFounderMetricsQuery(userId.Value, from, to), http.RequestAborted);
                await WriteServerSentEventAsync(http, "metrics", metrics);
            }
            while (await timer.WaitForNextTickAsync(http.RequestAborted));
        });

        // Live server-operations snapshot (runtime, dependency health, configured integrations, recent
        // errors). Restricted to the super-admin alone - the handler 403s everyone else, including
        // ordinary admins. The acting user comes from the session, never the request.
        group.MapGet("/server", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetServerDiagnosticsQuery(userId.Value)));
        });

        group.MapGet("/server/stream", async (
            HttpContext http,
            ISender sender,
            IServerTelemetryStore telemetry) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
            {
                http.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var current = telemetry.Current
                ?? await sender.Send(new GetServerDiagnosticsQuery(userId.Value), http.RequestAborted);
            await WriteEventStreamHeadersAsync(http);
            await WriteServerSentEventAsync(http, "history", new ServerTelemetryHistoryDto(
                current,
                telemetry.History(),
                telemetry.SampleIntervalSeconds,
                telemetry.RetentionMinutes));

            await foreach (var snapshot in telemetry.SubscribeAsync(http.RequestAborted))
                await WriteServerSentEventAsync(http, "snapshot", snapshot);
        });

        // Dismiss one line from the recent warning/error buffer. Super-admin only (handler 403s others).
        group.MapDelete("/server/logs/{logId:guid}", async (Guid logId, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var deleted = await sender.Send(new DeleteRecentLogCommand(userId.Value, logId));
            return Results.Ok(new { deleted });
        });

        // Clear the entire recent warning/error buffer. Super-admin only (handler 403s others).
        group.MapDelete("/server/logs", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var removed = await sender.Send(new ClearRecentLogsCommand(userId.Value));
            return Results.Ok(new { removed });
        });

        // Testing aid: pulls every word the admin has saved ("Mening so'zlarim") into the SRS review
        // queue by making each due right now, so the review flow can be exercised without waiting out
        // the 3/7/21-day schedule. Acts only on the caller's OWN words; the handler rejects
        // non-admins with a 403.
        group.MapPost("/force-due-reviews", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new ForceDueReviewsCommand(userId.Value)));
        });

        // Promote/demote another account. The handler rejects everyone but the super-admin with a 403.
        // The target id comes from the route and the acting user from the session - never the body.
        group.MapPut("/users/{targetId:guid}/admin", async (
            Guid targetId, SetAdminRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new SetUserAdminCommand(userId.Value, targetId, request.IsAdmin)));
        });

        // The recently sent broadcasts (operator history). The handler rejects non-super-admins with a 403.
        group.MapGet("/broadcasts", async (string? cursor, int? pageSize, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAdminBroadcastsQuery(
                userId.Value, cursor, Math.Min(pageSize ?? 20, 50))));
        });

        // Compose and send a broadcast notification to every learner. The author is the session user
        // (never the body), and the handler rejects everyone but the super-admin with a 403.
        group.MapPost("/broadcasts", async (
            SendBroadcastRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new SendAdminBroadcastCommand(
                userId.Value, request.Title, request.Body, request.LinkUrl)));
        });

        // Remove a previously sent broadcast from the operator history. The handler rejects everyone
        // but the super-admin with a 403; the id comes from the route and the actor from the session.
        group.MapDelete("/broadcasts/{broadcastId:guid}", async (
            Guid broadcastId, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var deleted = await sender.Send(new DeleteAdminBroadcastCommand(userId.Value, broadcastId));
            return Results.Ok(new { deleted });
        });

        // Manual "send now" fallback: runs the daily SRS reminder dispatch on demand. The handler
        // rejects everyone but the super-admin with a 403.
        group.MapPost("/notifications/dispatch-daily", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new TriggerDailyDispatchCommand(userId.Value)));
        });

        // Vocabulary topic administration (curated catalog). The acting user comes from the
        // session, never the request; the endpoint is guarded by the authenticated /api/admin
        // group (the global fallback policy requires a signed-in user), but that only proves the
        // caller is logged in, not that they're an admin - every command/query below carries the
        // acting user id and the handler itself authorizes the admin role (403 otherwise).
        group.MapGet("/vocabulary", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var list = await sender.Send(new GetAllVocabularyTopicsQuery(userId.Value));
            return Results.Ok(list);
        });

        group.MapGet("/vocabulary-images", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetVocabularyImagesAdminQuery(userId.Value)));
        });

        group.MapPost("/vocabulary-images/{topicId:guid}/{imageId:guid}/replace", async (
            Guid topicId,
            Guid imageId,
            HttpContext http,
            ISender sender,
            CancellationToken cancellationToken) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new ReplaceVocabularyImageAdminCommand(userId.Value, topicId, imageId),
                cancellationToken));
        });

        group.MapGet("/vocabulary/{id:guid}", async (
            Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetVocabularyTopicAdminQuery(userId.Value, id)));
        });

        // Create a new vocabulary topic. The request is bound from the body (the id is server-side,
        // never client-supplied); the handler rejects non-admins with a 403. Returns 201 with the
        // created topic's location.
        group.MapPost("/vocabulary", async (
            CreateVocabularyTopicRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new CreateVocabularyTopicCommand(
                userId.Value, request.Slug, request.Title, request.TitleUz, request.Category,
                request.GrammarFocusCode, request.Level, request.Sequence));
            return Results.Created($"/api/admin/vocabulary/{dto.Id}", dto);
        });

        // Update an existing vocabulary topic. The id comes from the route (never the body) and the
        // actor from the session; the handler rejects non-admins with a 403.
        group.MapPut("/vocabulary/{id:guid}", async (
            Guid id, UpdateVocabularyTopicRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new UpdateVocabularyTopicCommand(
                userId.Value, id, request.Title, request.TitleUz, request.Category,
                request.GrammarFocusCode, request.Sequence, request.Level, request.Passage,
                request.Words.Select(word => new UpdateVocabularyTopicWord(
                    word.Word, word.Translation, word.ExampleSentence, word.PartOfSpeech,
                    word.LexicalCategory, word.Register, word.UsageNote,
                    word.ImageUrl, word.ImageSource, word.ImageAttribution)).ToArray()));
            return Results.Ok(dto);
        });

        // Delete a vocabulary topic. The id comes from the route and the actor from the session;
        // the handler rejects non-admins with a 403.
        group.MapDelete("/vocabulary/{id:guid}", async (
            Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            await sender.Send(new DeleteVocabularyTopicCommand(userId.Value, id));
            return Results.NoContent();
        });

        // Grammar lesson administration (curated catalog). Mirrors the vocabulary endpoints:
        // the actor is the session user, the group is auth-guarded, and the handlers 403 non-admins.
        group.MapGet("/grammar", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAllGrammarLessonsQuery(userId.Value)));
        });

        group.MapGet("/grammar/{id:guid}", async (Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetGrammarLessonAdminQuery(userId.Value, id)));
        });

        group.MapPost("/grammar", async (
            GrammarLessonAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new CreateGrammarLessonCommand(
                userId.Value, request.Title, request.Category, request.Level));
            return Results.Created($"/api/admin/grammar/{dto.Id}", dto);
        });

        group.MapPut("/grammar/{id:guid}", async (
            Guid id, GrammarLessonAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new UpdateGrammarLessonCommand(
                userId.Value, id, request.Title, request.Category, request.Level));
            return Results.Ok(dto);
        });

        group.MapPut("/grammar/{id:guid}/full", async (
            Guid id, GrammarLessonAdminFullUpdateDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new UpdateGrammarLessonContentCommand(userId.Value, id, request)));
        });

        group.MapDelete("/grammar/{id:guid}", async (
            Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            await sender.Send(new DeleteGrammarLessonCommand(userId.Value, id));
            return Results.NoContent();
        });

        // Listening exercise administration (curated catalog). Same pattern: the handler, not this
        // endpoint, authorizes the caller's admin role.
        group.MapGet("/listening", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAllListeningExercisesQuery(userId.Value)));
        });

        group.MapGet("/listening/{id:guid}", async (Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new GetListeningExerciseAdminQuery(userId.Value, id)));
        });

        group.MapPost("/listening", async (
            ListeningExerciseAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new CreateListeningExerciseCommand(
                userId.Value, request.Title, request.Topic, request.Level));
            return Results.Created($"/api/admin/listening/{dto.Id}", dto);
        });

        group.MapPut("/listening/{id:guid}", async (
            Guid id, ListeningExerciseAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new UpdateListeningExerciseCommand(
                userId.Value, id, request.Title, request.Topic, request.Level));
            return Results.Ok(dto);
        });

        group.MapPut("/listening/{id:guid}/full", async (
            Guid id, ListeningExerciseAdminFullUpdateDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new UpdateListeningExerciseContentCommand(userId.Value, id, request)));
        });

        group.MapDelete("/listening/{id:guid}", async (
            Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            await sender.Send(new DeleteListeningExerciseCommand(userId.Value, id));
            return Results.NoContent();
        });

        // Reading passage administration (curated catalog). Same pattern: the handler, not this
        // endpoint, authorizes the caller's admin role.
        group.MapGet("/reading", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAllReadingPassagesQuery(userId.Value)));
        });

        group.MapGet("/reading/{id:guid}", async (
            Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new GetReadingPassageAdminQuery(userId.Value, id)));
        });

        group.MapPost("/reading", async (
            ReadingPassageAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new CreateReadingPassageCommand(
                userId.Value, request.Title, request.Topic, request.Level));
            return Results.Created($"/api/admin/reading/{dto.Id}", dto);
        });

        group.MapPut("/reading/{id:guid}", async (
            Guid id, ReadingPassageAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new UpdateReadingPassageCommand(
                userId.Value, id, request.Title, request.Topic, request.Level));
            return Results.Ok(dto);
        });

        group.MapPut("/reading/{id:guid}/full", async (
            Guid id, ReadingPassageAdminFullUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new UpdateReadingPassageFullCommand(userId.Value, id, request)));
        });

        group.MapDelete("/reading/{id:guid}", async (
            Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            await sender.Send(new DeleteReadingPassageCommand(userId.Value, id));
            return Results.NoContent();
        });

        // Writing task administration (curated catalog). Only the CEFR level is editable. Same
        // pattern: the handler, not this endpoint, authorizes the caller's admin role.
        group.MapGet("/writing", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetAllWritingTasksQuery(userId.Value)));
        });

        group.MapPost("/writing", async (
            WritingTaskAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new CreateWritingTaskCommand(userId.Value, request.Level));
            return Results.Created($"/api/admin/writing/{dto.Id}", dto);
        });

        group.MapPut("/writing/{id:guid}", async (
            Guid id, WritingTaskAdminUpsertDto request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            var dto = await sender.Send(new UpdateWritingTaskCommand(userId.Value, id, request.Level));
            return Results.Ok(dto);
        });

        group.MapDelete("/writing/{id:guid}", async (
            Guid id, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            await sender.Send(new DeleteWritingTaskCommand(userId.Value, id));
            return Results.NoContent();
        });

        group.MapGet("/books", async (HttpContext http, IAdminAuthorization admin, IBookRepository books, CancellationToken ct) =>
        {
            var userId = ResolveUserId(http.User); if (userId is null) return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, ct) is Application.Identity.Dtos.AdminRole.None) return Results.Forbid();
            return Results.Ok((await books.GetAllAsync(ct)).Select(AdminBookDto.FromDomain));
        });

        group.MapGet("/books/{id:guid}", async (Guid id, HttpContext http, IAdminAuthorization admin, IBookRepository books, CancellationToken ct) =>
        {
            var userId = ResolveUserId(http.User); if (userId is null) return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, ct) is Application.Identity.Dtos.AdminRole.None) return Results.Forbid();
            var book = await books.GetByIdAsync(id, ct); return book is null ? Results.NotFound() : Results.Ok(AdminBookDto.FromDomain(book));
        });

        group.MapPut("/books/{id:guid}", async (Guid id, UpdateAdminBookRequest request, HttpContext http,
            IAdminAuthorization admin, IBookRepository books, CancellationToken ct) =>
        {
            var userId = ResolveUserId(http.User); if (userId is null) return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, ct) is Application.Identity.Dtos.AdminRole.None) return Results.Forbid();
            var book = await books.GetByIdAsync(id, ct); if (book is null) return Results.NotFound();
            book.AdminUpdate(request.Title,request.TitleUz,request.Author,request.Synopsis,request.Topic,Enum.Parse<CefrLevel>(request.Level,true),request.CoverImageQuery);
            foreach (var sectionRequest in request.Sections)
            {
                var section = book.FindSection(sectionRequest.Id); if (section is null) return Results.BadRequest("Unknown section id.");
                section.AdminReplace(sectionRequest.Order, sectionRequest.Title, sectionRequest.Body,
                    sectionRequest.Questions.Select(q => BookQuestion.Create(q.Prompt,q.Options,q.CorrectOptionIndex,q.Explanation)));
            }
            await books.SaveAsync(book,ct); return Results.Ok(AdminBookDto.FromDomain(book));
        });

        group.MapGet("/curriculum/runs", async (HttpContext http, IAdminAuthorization admin,
            [Microsoft.AspNetCore.Mvc.FromServices] EnglishAiDbContext db, CancellationToken ct) =>
        {
            var userId = ResolveUserId(http.User); if (userId is null) return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, ct) is Application.Identity.Dtos.AdminRole.None) return Results.Forbid();

            var runs = await db.CurriculumGenerationRuns.AsNoTracking()
                .OrderByDescending(x => x.CreatedAt)
                .Select(x => new CurriculumRunAdminDto(
                    x.Id, x.Version, x.Provider, x.Model, x.Status.ToString(), x.CreatedAt,
                    x.UpdatedAt, x.CompletedAt, x.PublishedAt,
                    db.CurriculumGenerationItems.Count(i => i.RunId == x.Id),
                    db.CurriculumGenerationItems.Count(i => i.RunId == x.Id && i.Status == CurriculumItemStatus.Approved),
                    db.CurriculumGenerationItems.Count(i => i.RunId == x.Id && i.Status == CurriculumItemStatus.Published),
                    db.CurriculumGenerationItems.Count(i => i.RunId == x.Id &&
                        (i.Status == CurriculumItemStatus.NeedsRetry || i.Status == CurriculumItemStatus.FailedPermanent || i.Status == CurriculumItemStatus.NeedsHumanReview))))
                .ToListAsync(ct);
            return Results.Ok(runs);
        });

        group.MapGet("/curriculum/runs/{runId:guid}/items", async (Guid runId, string? status, string? module,
            HttpContext http, IAdminAuthorization admin,
            [Microsoft.AspNetCore.Mvc.FromServices] EnglishAiDbContext db, CancellationToken ct) =>
        {
            var userId = ResolveUserId(http.User); if (userId is null) return Results.Unauthorized();
            if (await admin.GetRoleAsync(userId.Value, ct) is Application.Identity.Dtos.AdminRole.None) return Results.Forbid();

            var query = db.CurriculumGenerationItems.AsNoTracking().Where(x => x.RunId == runId);
            if (!string.IsNullOrWhiteSpace(module)) query = query.Where(x => x.Module == module);
            if (Enum.TryParse<CurriculumItemStatus>(status, true, out var parsedStatus)) query = query.Where(x => x.Status == parsedStatus);
            var items = await query.OrderBy(x => x.Module).ThenBy(x => x.SubjectKey)
                .Select(x => new CurriculumItemAdminDto(x.Id, x.Module, x.SubjectKey, x.Level == null ? null : x.Level.ToString(),
                    x.Status.ToString(), x.AttemptCount, x.ErrorCode, x.ErrorMessage, x.UpdatedAt))
                .ToListAsync(ct);
            return Results.Ok(items);
        });
    }

    private static Task WriteEventStreamHeadersAsync(HttpContext http)
    {
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache, no-transform";
        http.Response.Headers.Connection = "keep-alive";
        http.Response.Headers["X-Accel-Buffering"] = "no";
        return http.Response.Body.FlushAsync(http.RequestAborted);
    }

    private static async Task WriteServerSentEventAsync(HttpContext http, string eventName, object payload)
    {
        var json = JsonSerializer.Serialize(payload, new JsonSerializerOptions(JsonSerializerDefaults.Web));
        await http.Response.WriteAsync($"event: {eventName}\ndata: {json}\n\n", http.RequestAborted);
        await http.Response.Body.FlushAsync(http.RequestAborted);
    }

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public sealed record SetAdminRequest(bool IsAdmin);

    public sealed record SetVariableCostBudgetRequest(double DailyBudgetUsd);

    public sealed record SendBroadcastRequest(string Title, string Body, string? LinkUrl);

    public sealed record CreateVocabularyTopicRequest(
        string Slug, string Title, string TitleUz, string Category,
        string GrammarFocusCode, string Level, int Sequence = 0);

    public sealed record UpdateVocabularyTopicRequest(
        string Title, string TitleUz, string Category,
        string GrammarFocusCode, int Sequence, string Level, string Passage,
        IReadOnlyList<UpdateVocabularyTopicWordRequest> Words);

    public sealed record UpdateVocabularyTopicWordRequest(
        string Word, string Translation, string? ExampleSentence, string PartOfSpeech,
        string? LexicalCategory, string? Register, string? UsageNote,
        string? ImageUrl, string? ImageSource, string? ImageAttribution);
    public sealed record UpdateAdminBookQuestionRequest(string Prompt, IReadOnlyList<string> Options, int CorrectOptionIndex, string? Explanation);
    public sealed record UpdateAdminBookSectionRequest(Guid Id, int Order, string Title, string Body, IReadOnlyList<UpdateAdminBookQuestionRequest> Questions);
    public sealed record UpdateAdminBookRequest(string Title, string TitleUz, string Author, string Synopsis, string Topic,
        string Level, string CoverImageQuery, IReadOnlyList<UpdateAdminBookSectionRequest> Sections);
    public sealed record CurriculumRunAdminDto(Guid Id, string Version, string Provider, string Model, string Status,
        DateTimeOffset CreatedAt, DateTimeOffset UpdatedAt, DateTimeOffset? CompletedAt, DateTimeOffset? PublishedAt,
        int TotalItems, int ApprovedItems, int PublishedItems, int ProblemItems);
    public sealed record CurriculumItemAdminDto(Guid Id, string Module, string SubjectKey, string? Level, string Status,
        int AttemptCount, string? ErrorCode, string? ErrorMessage, DateTimeOffset UpdatedAt);
}
