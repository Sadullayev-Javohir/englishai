using Application.Translation.TranslateText;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for cache-first, on-demand translation of English teaching text into Uzbek.
/// </summary>
public static class TranslationEndpoints
{
    public static IEndpointRouteBuilder MapTranslationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/translate").WithTags("Translation");

        group.MapPost("/", async (TranslateRequest request, ISender sender) =>
            Results.Ok(await sender.Send(new TranslateTextQuery(
                request.Text, request.Level, request.Speaker, request.Topic, request.PreviousTurns))));

        return app;
    }

    /// <summary>One sentence/short text to translate into Uzbek, with an optional CEFR level hint.</summary>
    public sealed record TranslateRequest(
        string Text,
        CefrLevel? Level,
        string? Speaker = null,
        string? Topic = null,
        IReadOnlyList<string>? PreviousTurns = null);
}
