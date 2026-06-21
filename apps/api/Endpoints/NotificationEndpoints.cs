using Application.Notifications.RegisterDevice;
using MediatR;

namespace Web.Endpoints;

/// <summary>
/// Push-notification device registration. Thin: forwards to MediatR. Ownership is enforced by the
/// pipeline (the command's <c>LearnerId</c> is checked against the JWT), so a learner can only
/// register a token under their own account.
/// </summary>
public static class NotificationEndpoints
{
    /// <summary>Body for device registration; the learner id comes from the route/JWT.</summary>
    public sealed record RegisterDeviceRequest(string Token, string Platform);

    public static IEndpointRouteBuilder MapNotificationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/notifications").WithTags("Notifications");

        group.MapPost("/{learnerId:guid}/register-device", async (
            Guid learnerId, RegisterDeviceRequest request, ISender sender) =>
        {
            await sender.Send(new RegisterDeviceCommand(learnerId, request.Token, request.Platform));
            return Results.NoContent();
        });

        return app;
    }
}
