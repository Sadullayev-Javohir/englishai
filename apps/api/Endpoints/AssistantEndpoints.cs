using Application.Assistant.AskAssistant;
using Application.Assistant.AskProjectAssistant;
using Application.Assistant.Contextual;
using Application.Assistant.Dtos;
using Application.Assistant.Sessions;
using Application.Ai;
using Infrastructure.Video;
using MediatR;

namespace Web.Endpoints;

public static class AssistantEndpoints
{
    public sealed record AskAssistantRequest(string Question, IReadOnlyList<AssistantTurnDto> History);
    public sealed record AskProjectAssistantRequest(string Question, IReadOnlyList<AssistantTurnDto> History, string Locale = "uz");
    public sealed record AskContextualAssistantRequest(
        string Area,
        string Title,
        string Context,
        string FocusText,
        string Question,
        IReadOnlyList<ContextualAssistantTurnDto> History);
    public sealed record RenameAssistantSessionRequest(string Title);

    public static IEndpointRouteBuilder MapAssistantEndpoints(this IEndpointRouteBuilder app)
    {
        var sessions = app.MapGroup("/api/assistant/sessions").WithTags("Assistant").RequireAuthorization();
        sessions.MapGet("/", async (string? cursor, int? pageSize, IAssistantSessionService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.ListAsync(cursor, Math.Min(pageSize ?? 20, 50), cancellationToken)));
        sessions.MapPost("/", async (CreateAssistantSessionRequest request, IAssistantSessionService service, CancellationToken cancellationToken) =>
        {
            var created = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/assistant/sessions/{created.Id}", created);
        });
        sessions.MapGet("/{sessionId:guid}", async (Guid sessionId, IAssistantSessionService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAsync(sessionId, cancellationToken)));
        sessions.MapPatch("/{sessionId:guid}", async (Guid sessionId, RenameAssistantSessionRequest request, IAssistantSessionService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.RenameAsync(sessionId, request.Title, cancellationToken)));
        sessions.MapDelete("/{sessionId:guid}", async (Guid sessionId, IAssistantSessionService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(sessionId, cancellationToken);
            return Results.NoContent();
        });
        sessions.MapPost("/{sessionId:guid}/messages/stream", StreamSessionMessageAsync);

        app.MapPost("/api/assistant/ask", async (AskAssistantRequest request, ISender sender) =>
            Results.Ok(await sender.Send(new AskAssistantQuery(request.Question, request.History))))
            .WithTags("Assistant");

        app.MapPost("/api/assistant/project", async (AskProjectAssistantRequest request, ISender sender) =>
            Results.Ok(await sender.Send(new AskProjectAssistantQuery(request.Question, request.History, request.Locale))))
            .WithTags("Assistant")
            .AllowAnonymous();

        app.MapPost("/api/assistant/context/stream", async (
            AskContextualAssistantRequest request,
            ISender sender,
            HttpContext http) =>
        {
            http.Response.ContentType = "text/event-stream";
            http.Response.Headers.CacheControl = "no-cache";
            http.Response.Headers.Connection = "keep-alive";
            try
            {
                var result = await sender.Send(new AskContextualAssistantQuery(
                    request.Area, request.Title, request.Context, request.FocusText,
                    request.Question, request.History), http.RequestAborted);
                var reply = string.IsNullOrWhiteSpace(result.ReplyUz)
                    ? "Savolingizni dars konteksti bilan bosqichma-bosqich tahlil qiling va bitta sodda misolda qo‘llang."
                    : result.ReplyUz;
                var json = System.Text.Json.JsonSerializer.Serialize(new { text = reply });
                await http.Response.WriteAsync($"event: chunk\ndata: {json}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
                await http.Response.WriteAsync("event: done\ndata: {}\n\n", http.RequestAborted);
            }
            catch (AiAdmissionException exception)
            {
                http.Response.Headers.RetryAfter = Math.Max(1, exception.RetryAfterSeconds).ToString();
                var correlationId = http.Items.TryGetValue(Web.Observability.CorrelationConstants.ItemKey, out var value)
                    ? value?.ToString() ?? http.TraceIdentifier : http.TraceIdentifier;
                var json = System.Text.Json.JsonSerializer.Serialize(new
                {
                    code = exception.Code,
                    message = exception.Message,
                    retryAfterSeconds = exception.RetryAfterSeconds,
                    correlationId,
                });
                await http.Response.WriteAsync($"event: error\ndata: {json}\n\n", http.RequestAborted);
            }
        }).WithTags("Assistant");

        return app;
    }

    private static async Task StreamSessionMessageAsync(
        Guid sessionId,
        SendAssistantMessageRequest request,
        IAssistantSessionService service,
        HttpContext http)
    {
        http.Response.ContentType = "text/event-stream";
        http.Response.Headers.CacheControl = "no-cache";
        http.Response.Headers.Connection = "keep-alive";
        try
        {
            var result = await service.SendAsync(sessionId, request, http.RequestAborted);
            foreach (var chunk in ChunkReply(result.AssistantMessage.Text))
            {
                var chunkJson = System.Text.Json.JsonSerializer.Serialize(new { text = chunk });
                await http.Response.WriteAsync($"event: chunk\ndata: {chunkJson}\n\n", http.RequestAborted);
                await http.Response.Body.FlushAsync(http.RequestAborted);
            }
            var json = System.Text.Json.JsonSerializer.Serialize(new { message = result.AssistantMessage });
            await http.Response.WriteAsync($"event: done\ndata: {json}\n\n", http.RequestAborted);
            await http.Response.Body.FlushAsync(http.RequestAborted);
            await http.Response.CompleteAsync();
        }
        catch (AiAdmissionException exception)
        {
            await WriteStreamErrorAsync(http, exception.Code, exception.Message, exception.RetryAfterSeconds);
        }
        catch (AssistantUnavailableException exception)
        {
            await WriteStreamErrorAsync(http, exception.Code, exception.Message, exception.RetryAfterSeconds);
        }
    }

    private static async Task WriteStreamErrorAsync(HttpContext http, string code, string message, int retryAfterSeconds)
    {
        http.Response.Headers.RetryAfter = Math.Max(1, retryAfterSeconds).ToString();
        var correlationId = http.Items.TryGetValue(Web.Observability.CorrelationConstants.ItemKey, out var value)
            ? value?.ToString() ?? http.TraceIdentifier : http.TraceIdentifier;
        var json = System.Text.Json.JsonSerializer.Serialize(new
        {
            code,
            message,
            retryAfterSeconds = Math.Max(1, retryAfterSeconds),
            correlationId,
        });
        await http.Response.WriteAsync($"event: error\ndata: {json}\n\n", http.RequestAborted);
        await http.Response.Body.FlushAsync(http.RequestAborted);
        await http.Response.CompleteAsync();
    }

    private static IEnumerable<string> ChunkReply(string text)
    {
        const int targetSize = 48;
        for (var offset = 0; offset < text.Length;)
        {
            var remaining = text.Length - offset;
            if (remaining <= targetSize)
            {
                yield return text[offset..];
                yield break;
            }

            var end = Math.Min(text.Length, offset + targetSize);
            var boundary = text.LastIndexOf(' ', end - 1, end - offset);
            if (boundary <= offset) boundary = end;
            else boundary++;
            yield return text[offset..boundary];
            offset = boundary;
        }
    }

}
