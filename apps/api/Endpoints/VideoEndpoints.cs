using Application.Video.Dtos;
using Application.Video.ExplainSegment;
using Application.Video.GetRealtimeTranscript;
using Application.Video.GetVideoCatalog;
using Application.Video.GetVideoFeed;
using Application.Video.GetVideoLesson;
using Application.Video.FindFullVideoPlaylist;
using Application.Video.IngestVideo;
using Application.Video.OpenVideo;
using Application.Video.OpenVideoByUrl;
using Application.Video.Ports;
using Application.Video.RateVideoDifficulty;
using Application.Video.SearchVideo;
using Application.Video.SubmitVideoQuiz;
using Application.Video.GenerateVideoQuiz;
using Application.Video.Models;
using Application.Vocabulary.LearnWord;
using Domain.Vocabulary;
using Infrastructure.Jobs;
using Infrastructure.Video;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the Video/Listening module (PROJECT-SPEC Faza 4). Endpoints are thin:
/// they forward to MediatR. Ingestion is enqueued onto Hangfire when available so the
/// quota-limited YouTube/LLM work runs off the request path (docs/development-guide.md rule 17.3).
/// </summary>
public static class VideoEndpoints
{
    /// <summary>Default feed page size when the client does not specify one.</summary>
    private const int DefaultFeedPageSize = 12;

    /// <summary>Request to save a word encountered in a video into the SRS (Faza 3 link).</summary>
    public sealed record SaveVideoWordRequest(Guid LearnerId, string Word, string Translation, string? ExampleSentence);

    /// <summary>Request to ingest a new YouTube video into the curated catalog.</summary>
    public sealed record IngestVideoRequest(string YouTubeVideoId, string Topic);
    public sealed record GenerateQuizRequest(Guid LearnerId);

    /// <summary>One explain-chat question about a video, with current-line focus and trailing history.</summary>
    public sealed record ExplainRequest(
        Guid VideoLessonId, string FocusText, string UserMessage, IReadOnlyList<ChatTurnDto> History);

    public static IEndpointRouteBuilder MapVideoEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/video").WithTags("Video");

        group.MapGet("/catalog/{learnerId:guid}", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetVideoCatalogQuery(learnerId))));

        // Infinite, level-adaptive feed (PROJECT-SPEC B.3, Bosqich 2). The client sends back
        // the previous response's NextCursor to load the next page as the learner scrolls.
        group.MapGet("/feed/{learnerId:guid}", async (
            Guid learnerId, ISender sender, string? cursor, int? pageSize, string? visitSeed) =>
            Results.Ok(await sender.Send(new GetVideoFeedQuery(
                learnerId, cursor, pageSize ?? DefaultFeedPageSize, visitSeed))));

        // Learner-typed search (the catalog's search box): English-only, safe-search strict
        // results for any query, plus an opaque cursor for "load more". Adult/pornographic
        // results are filtered out server-side before they reach the learner.
        group.MapGet("/search", async (
            string q, ISender sender, string? cursor, int? pageSize) =>
            Results.Ok(await sender.Send(new SearchVideoQuery(q, cursor, pageSize ?? DefaultFeedPageSize))));

        // Long-form film/cartoon discovery. This endpoint returns only duration-checked
        // collections, so short clips are never presented as a complete movie or playlist.
        group.MapGet("/playlist/search", async (string q, ISender sender) =>
            Results.Ok(await sender.Send(new FindFullVideoPlaylistQuery(q))));
        group.MapGet("/playlist/featured", async (ISender sender) =>
            Results.Ok(await sender.Send(new GetFeaturedVideoPlaylistQuery())));
        group.MapGet("/playlist/{playlistId}", async (string playlistId, ISender sender) =>
            Results.Ok(await sender.Send(new GetFullVideoPlaylistQuery(playlistId))));

        // Opens a feed video into a playable lesson (creating it on first open). Returns the
        // lesson so the client can navigate to its preview/player.
        group.MapPost("/open", async (OpenVideoCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        // Opens an arbitrary YouTube video the learner pasted (its extracted video id) into a
        // playable lesson, returning it so the client can navigate straight to the player.
        group.MapPost("/open-url", async (OpenVideoByUrlCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapGet("/{videoLessonId:guid}", async (Guid videoLessonId, ISender sender) =>
            Results.Ok(await sender.Send(new GetVideoLessonQuery(videoLessonId))));

        // Real-time transcript for a learner-pasted video, fetched live through the orchestrator and
        // returned WITHOUT being stored (docs/development-guide.md real-time rule - arbitrary user videos never bloat
        // the DB). Status is "available" / "unavailable" / "pending"; the client picks the Uzbek message.
        group.MapGet("/transcript/{youTubeVideoId}", async (string youTubeVideoId, ISender sender) =>
            Results.Ok(await sender.Send(new GetRealtimeTranscriptQuery(youTubeVideoId))));

        group.MapPost("/{id:guid}/quiz", async (Guid id, GenerateQuizRequest request, ISender sender, CancellationToken ct) =>
        {
            try { return Results.Ok(await sender.Send(new GenerateVideoQuizCommand(id, request.LearnerId), ct)); }
            catch (VideoQuizUnavailableException ex) { return QuizUnavailable(ex); }
        });
        group.MapGet("/quiz/{quizId:guid}", async (Guid quizId, Guid learnerId, ISender sender, CancellationToken ct) =>
        {
            try { return Results.Ok(await sender.Send(new GetGeneratedVideoQuizQuery(quizId, learnerId), ct)); }
            catch (VideoQuizUnavailableException ex) { return QuizUnavailable(ex); }
        });
        group.MapPost("/quiz", async (SubmitVideoQuizCommand command, ISender sender, CancellationToken ct) =>
        {
            try { return Results.Ok(await sender.Send(command, ct)); }
            catch (VideoQuizUnavailableException ex) { return QuizUnavailable(ex); }
        });

        // Explain-chat: "why is this word/phrase used" style free-text Q&A about a real transcript
        // line, answered in Uzbek (rule 11's translation exception, widened - see IChatExplainer).
        group.MapPost("/explain", async (ExplainRequest request, ISender sender) =>
        {
            try
            {
                var reply = await sender.Send(new ExplainSegmentQuery(
                    request.VideoLessonId, request.FocusText, request.UserMessage, request.History));
                return reply.ReplyUz is null
                    ? Results.Json(reply, statusCode: StatusCodes.Status503ServiceUnavailable)
                    : Results.Ok(reply);
            }
            catch (VideoExplainBusyException exception)
            {
                return Results.Json(
                    new { error = exception.Code, message = exception.Message },
                    statusCode: exception.Code == "user_rate_limited" ? 429 : 503);
            }
        });

        group.MapGet("/explain/metrics", (IVideoExplainCoordinator coordinator) =>
            Results.Ok(coordinator.Snapshot())).AllowAnonymous();

        group.MapPost("/explain/stream", async (
            ExplainRequest request,
            ISender sender,
            HttpContext http) =>
        {
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers.Connection = "keep-alive";

            try
            {
                var result = await sender.Send(new ExplainSegmentQuery(
                    request.VideoLessonId, request.FocusText, request.UserMessage, request.History),
                    http.RequestAborted);

                if (string.IsNullOrWhiteSpace(result.ReplyUz))
                {
                    await http.Response.WriteAsync("event: error\ndata: unavailable\n\n", http.RequestAborted);
                    return;
                }

                foreach (var chunk in Chunk(result.ReplyUz, 24))
                {
                    var json = System.Text.Json.JsonSerializer.Serialize(new { text = chunk });
                    await http.Response.WriteAsync($"event: chunk\ndata: {json}\n\n", http.RequestAborted);
                    await http.Response.Body.FlushAsync(http.RequestAborted);
                }

                await http.Response.WriteAsync("event: done\ndata: {}\n\n", http.RequestAborted);
            }
            catch (VideoExplainBusyException exception)
            {
                var json = System.Text.Json.JsonSerializer.Serialize(new { code = exception.Code, message = exception.Message });
                await http.Response.WriteAsync($"event: error\ndata: {json}\n\n", http.RequestAborted);
            }
        });

        group.MapPost("/rate", async (RateVideoDifficultyCommand command, ISender sender) =>
        {
            await sender.Send(command);
            return Results.NoContent();
        });

        // Saving a word from a video feeds it straight into the SRS as a Video-sourced item,
        // whose first mini-test is listening recognition (PROJECT-SPEC Faza 4 ↔ Faza 3).
        group.MapPost("/word", async (SaveVideoWordRequest request, ISender sender) =>
            Results.Ok(await sender.Send(new LearnWordCommand(
                request.LearnerId, request.Word, request.Translation,
                request.ExampleSentence, VocabularySource.Video))));

        group.MapPost("/ingest", async (IngestVideoRequest request, ISender sender, IServiceProvider services) =>
        {
            var jobs = services.GetService<IBackgroundJobScheduler>();
            if (jobs is not null)
            {
                jobs.EnqueueContent<VideoIngestionJob>(job => job.RunAsync(request.YouTubeVideoId, request.Topic));
                return Results.Accepted();
            }

            return Results.Ok(await sender.Send(new IngestVideoCommand(request.YouTubeVideoId, request.Topic)));
        });

        return app;
    }

    private static IEnumerable<string> Chunk(string text, int size)
    {
        for (var offset = 0; offset < text.Length; offset += size)
            yield return text.Substring(offset, Math.Min(size, text.Length - offset));
    }

    private static IResult QuizUnavailable(VideoQuizUnavailableException exception) =>
        Results.Json(new { error = exception.Code }, statusCode: exception.Code == "quiz_expired" ? 410 :
            exception.Code == "transcript_unavailable" ? 409 : 503);
}
