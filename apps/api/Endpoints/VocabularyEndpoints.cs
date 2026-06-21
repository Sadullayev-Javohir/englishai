using Application.Notifications.DismissNotification;
using Application.Notifications.GetNotifications;
using Application.Notifications.MarkNotificationsRead;
using Application.Vocabulary.CheckWordPronunciation;
using Application.Vocabulary.GetDueReviews;
using Application.Vocabulary.GetMandatoryReviewStatus;
using Application.Vocabulary.GetTopicCompletion;
using Application.Vocabulary.GetVocabulary;
using Application.Vocabulary.GetVocabularyTopic;
using Application.Vocabulary.GetVocabularyTopics;
using Application.Vocabulary.LearnWord;
using Application.Vocabulary.GetPassageTranslation;
using Application.Vocabulary.RecordTopicModuleScore;
using Application.Vocabulary.ResetTopicModuleScore;
using Application.Vocabulary.RemoveSavedWord;
using Application.Vocabulary.SubmitReview;
using Application.Vocabulary.SubmitTopicQuiz;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for the vocabulary SRS and in-app notifications (PROJECT-SPEC Faza 3).
/// Endpoints are thin: they forward to MediatR and return the result. No business logic.
/// </summary>
public static class VocabularyEndpoints
{
    /// <summary>Request to check a learner's spoken pronunciation of a target word.</summary>
    /// <remarks><c>AudioContent</c> is base64 in the JSON body; the command binds it to a byte[].</remarks>
    public sealed record CheckPronunciationRequest(string Word, byte[] AudioContent);

    public static IEndpointRouteBuilder MapVocabularyEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/vocabulary").WithTags("Vocabulary");

        // Module 4: vocabulary-in-context topics (passage + 15 words + quiz + pronunciation check).
        // `level` browses one CEFR band; `all=true` returns the whole A1→C2 catalog easiest-first.
        group.MapGet("/topics/{learnerId:guid}", async (
            Guid learnerId, CefrLevel? level, bool? all, ISender sender) =>
            Results.Ok(await sender.Send(new GetVocabularyTopicsQuery(learnerId, level, all ?? false))));

        group.MapGet("/topic/{topicId:guid}", async (Guid topicId, ISender sender) =>
            Results.Ok(await sender.Send(new GetVocabularyTopicQuery(topicId))));

        // The "MATN" card: the passage split sentence-by-sentence, each paired with its Uzbek
        // translation. Translation is cache-first and never fabricated (rules 8/10/11).
        group.MapPost("/topic/{topicId:guid}/passage-translation", async (
            Guid topicId, ISender sender) =>
            Results.Ok(await sender.Send(new GetPassageTranslationQuery(topicId))));

        group.MapPost("/topic/quiz", async (SubmitTopicQuizCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        // K.5 topic mastery across the six modules: read the checklist, and record a module score
        // for flows that aren't yet wired directly (the Vocabulary quiz records its own score).
        group.MapGet("/topic/{topicId:guid}/completion/{learnerId:guid}",
            async (Guid topicId, Guid learnerId, ISender sender) =>
                Results.Ok(await sender.Send(new GetTopicCompletionQuery(learnerId, topicId))));

        group.MapPost("/topic/completion", async (RecordTopicModuleScoreCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/topic/completion/reset", async (ResetTopicModuleScoreCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/pronounce", async (CheckPronunciationRequest request, ISender sender) =>
            Results.Ok(await sender.Send(
                new CheckWordPronunciationCommand(request.Word, request.AudioContent))));

        group.MapPost("/learn", async (LearnWordCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapPost("/review", async (SubmitReviewCommand command, ISender sender) =>
            Results.Ok(await sender.Send(command)));

        group.MapGet("/{learnerId:guid}", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetVocabularyQuery(learnerId))));

        group.MapDelete("/{learnerId:guid}/{vocabularyItemId:guid}", async (
            Guid learnerId, Guid vocabularyItemId, ISender sender) =>
        {
            await sender.Send(new RemoveSavedWordCommand(learnerId, vocabularyItemId));
            return Results.NoContent();
        });

        group.MapGet("/{learnerId:guid}/due", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetDueReviewsQuery(learnerId))));

        group.MapGet("/{learnerId:guid}/review-status", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetMandatoryReviewStatusQuery(learnerId))));

        group.MapGet("/{learnerId:guid}/notifications", async (
            Guid learnerId, string? cursor, int? pageSize, ISender sender) =>
            Results.Ok(await sender.Send(new GetNotificationsQuery(
                learnerId, cursor, Math.Min(pageSize ?? 30, 50)))));

        group.MapPost("/{learnerId:guid}/notifications/read", async (Guid learnerId, ISender sender) =>
        {
            await sender.Send(new MarkNotificationsReadCommand(learnerId));
            return Results.NoContent();
        });

        // Dismiss a single tapped notification: it opens its destination on the client and drops out
        // of the feed, while everything else stays.
        group.MapPost("/{learnerId:guid}/notifications/{notificationId:guid}/read",
            async (Guid learnerId, Guid notificationId, ISender sender) =>
            {
                await sender.Send(new DismissNotificationCommand(learnerId, notificationId));
                return Results.NoContent();
            });

        return app;
    }
}
