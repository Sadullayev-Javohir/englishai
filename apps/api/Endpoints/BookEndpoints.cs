using Application.Books.CheckBookAnswer;
using Application.Books.GetBook;
using Application.Books.GetBooksCatalog;
using Application.Books.GetBookSection;
using Application.Books.SubmitBookQuiz;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the Books library (Home → Books). Thin endpoints that forward to MediatR.
/// A book is a multi-section graded reader; each section is read once at least 70% of its quiz is
/// correct, and the whole book is confirmed read once every section is passed.
/// </summary>
public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/books").WithTags("Books");

        // `level` browses one CEFR band; `all=true` returns the whole library easiest-first;
        // no filter adapts to the learner's level. Each row carries the learner's progress.
        group.MapGet("/catalog/{learnerId:guid}", async (
            Guid learnerId, CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetBooksCatalogQuery(learnerId, level, all ?? false))));

        // A book's detail page: metadata + table of contents with per-section read state.
        group.MapGet("/{bookId:guid}/learner/{learnerId:guid}", async (
            Guid bookId, Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetBookQuery(bookId, learnerId))));

        // One section's reader: body + answerless quiz; generated and cached on first open.
        group.MapGet("/{bookId:guid}/sections/{sectionId:guid}/learner/{learnerId:guid}", async (
            Guid bookId, Guid sectionId, Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetBookSectionQuery(bookId, sectionId, learnerId))));

        group.MapPost("/answers/check", async (CheckBookAnswerCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        // Submit a section's comprehension quiz for server-side grading (pass at 70%).
        group.MapPost("/quiz", async (SubmitBookQuizCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        return app;
    }
}
