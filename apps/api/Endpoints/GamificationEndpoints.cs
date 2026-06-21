using Application.Gamification.CompleteTask;
using Application.Gamification.ConsumeEnergy;
using Application.Gamification.GetEnergy;
using Domain.Gamification;
using Application.Gamification.GetGamificationStatus;
using Application.Gamification.GetLeaderboard;
using Application.Gamification.GetPointsBalance;
using Application.Gamification.RedeemDiscount;
using Domain.Assessment;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// HTTP surface for daily-goal/streak gamification and the leaderboard/points feature
/// (PROJECT-SPEC Faza 5). Thin: forwards to MediatR, no business logic.
/// </summary>
public static class GamificationEndpoints
{
    public static IEndpointRouteBuilder MapGamificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/gamification").WithTags("Gamification");

        // Other modules call this when the learner finishes a task (Speaking turn, SRS
        // review, finished video lesson, etc.) to advance the daily goal and streak.
        group.MapPost("/{learnerId:guid}/complete-task", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new CompleteTaskCommand(learnerId))));

        group.MapGet("/{learnerId:guid}", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetGamificationStatusQuery(learnerId))));

        // Top 50 of a CEFR level's leaderboard plus the caller's own rank. `level` defaults to
        // the learner's own current CEFR level when omitted (opening the page with no tab pick).
        group.MapGet("/{learnerId:guid}/leaderboard", async (Guid learnerId, CefrLevel? level, ISender sender) =>
            Results.Ok(await sender.Send(new GetLeaderboardQuery(learnerId, level))));

        group.MapGet("/{learnerId:guid}/points", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetPointsBalanceQuery(learnerId))));

        group.MapGet("/{learnerId:guid}/energy", async (Guid learnerId, ISender sender) =>
            Results.Ok(await sender.Send(new GetEnergyQuery(learnerId))));

        group.MapPost("/{learnerId:guid}/energy/consume", async (
            Guid learnerId, ConsumeEnergyRequest body, ISender sender) =>
        {
            if (!Enum.TryParse<EnergyAction>(body.Action, ignoreCase: true, out var action))
            {
                return Results.ValidationProblem(new Dictionary<string, string[]>
                {
                    ["action"] = ["Action must be video or speaking."],
                });
            }

            return Results.Ok(await sender.Send(
                new ConsumeEnergyCommand(learnerId, action, body.ReferenceId)));
        });

        group.MapPost("/{learnerId:guid}/redeem-discount", async (
            Guid learnerId, RedeemDiscountRequest body, ISender sender) =>
            Results.Ok(await sender.Send(new RedeemDiscountCommand(learnerId, body.CoinsCost))));

        return app;
    }
}

public sealed record RedeemDiscountRequest(int CoinsCost);
/// <param name="ReferenceId">YouTube video id for Video, conversation session id for Speaking.</param>
public sealed record ConsumeEnergyRequest(string Action, string ReferenceId);
