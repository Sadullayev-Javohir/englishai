using Application.Reading.CheckReadingAnswer;
using Application.Reading.GetReadingCatalog;
using Application.Reading.GetReadingPassage;
using Application.Reading.SubmitReadingQuiz;
using Application.Vocabulary.LearnWord;
using Domain.Assessment;
using Domain.Vocabulary;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the Reading module (PROJECT-SPEC Faza 6 - leveled text + interactive
/// glossary). Endpoints are thin: they forward to MediatR and return the result.
/// </summary>
public static class ReadingEndpoints
{
    /// <summary>Request to save a glossed word from a passage into the SRS (Faza 3 link).</summary>
    public sealed record SaveReadingWordRequest(Guid LearnerId, string Word, string Translation, string? ExampleSentence);

    public static IEndpointRouteBuilder MapReadingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/reading").WithTags("Reading");

        // `level` browses one CEFR band; `all=true` returns the whole catalog easiest-first.
        group.MapGet("/catalog/{learnerId:guid}", async (
            Guid learnerId, CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetReadingCatalogQuery(learnerId, level, all ?? false))));

        // The catalog key is the learning-spine topic id; the lesson is generated + cached on open.
        group.MapGet("/topic/{topicId:guid}", async (Guid topicId, ISender sender) =>
            Results.Ok(await sender.Send(new GetReadingPassageQuery(topicId))));

        group.MapPost("/quiz/check", async (CheckReadingAnswerQuery query, ISender sender) =>
            Results.Ok(await sender.Send(query)));

        group.MapPost("/quiz", async (SubmitReadingQuizCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        // Saving a glossed word feeds it into the SRS as a Reading-sourced item
        // (PROJECT-SPEC Faza 6 interactive glossary ↔ Faza 3 SRS).
        group.MapPost("/word", async (SaveReadingWordRequest request, ISender sender) =>
            Results.Ok(await sender.Send(new LearnWordCommand(
                request.LearnerId, request.Word, request.Translation,
                request.ExampleSentence, VocabularySource.Reading))));

        return app;
    }
}
