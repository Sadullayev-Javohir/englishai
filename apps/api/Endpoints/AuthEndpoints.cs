using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Application.Identity.AuthenticateWithGoogle;
using Application.Identity.DevLogin;
using Application.Identity.CheckUsernameAvailability;
using Application.Identity.DeleteAccount;
using Application.Identity.Dtos;
using Application.Identity.GetCurrentUser;
using Application.Identity.GetUserPreferences;
using Application.Identity.SetLearningGoal;
using Application.Identity.SetDemographics;
using Application.Identity.SetPreferredName;
using Application.Identity.UpdateProfile;
using Application.Identity.UpdateAvatar;
using Application.Storage;
using Infrastructure.Storage;
using Application.Identity.DeleteAvatar;
using Application.Identity.Avatar;
using Infrastructure.Identity.Avatar;
using Application.Identity.UpdateUserPreferences;
using Domain.Identity;
using Infrastructure.Identity;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using System.Text.Json;
using Web.Auth;

namespace Web.Endpoints;

/// <summary>
/// Authentication surface (docs/development-guide.md §3 - Google OAuth 2.0 + JWT in an HttpOnly cookie).
/// The SPA obtains a Google ID token via Google Identity Services and posts it here; the
/// server verifies it, upserts the account, and returns a session cookie. Thin: all logic
/// lives in the MediatR handlers.
/// </summary>
public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth").WithTags("Auth");

        // OAuth client ids are public identifiers, not secrets. Returning the backend's
        // configured value keeps the browser's token audience aligned with server validation
        // even when the static frontend image was built without a Vite build argument.
        group.MapGet("/config", (GoogleAuthOptions google) =>
            Results.Ok(new AuthConfigResponse(google.ClientId?.Trim() ?? string.Empty)))
            .AllowAnonymous();

        // Native OAuth bridge: Google Identity Services runs on the already-authorized HTTPS web
        // origin, then hands a short-lived one-use code back to the app's custom URL scheme. This
        // keeps native login independent from Android signing-certificate OAuth registration.
        group.MapGet("/mobile-google", (string state, GoogleAuthOptions google) =>
        {
            if (string.IsNullOrWhiteSpace(state) || state.Length > 200 || !google.IsConfigured)
                return Results.BadRequest();

            var clientId = JsonSerializer.Serialize(google.ClientId!.Trim());
            var safeState = JsonSerializer.Serialize(state);
            var html = """
                <!doctype html><html lang="uz"><head><meta charset="utf-8">
                <meta name="viewport" content="width=device-width,initial-scale=1">
                <title>EnglishAI.uz - Google bilan kirish</title>
                <script src="https://accounts.google.com/gsi/client" async></script>
                <style>body{font-family:system-ui;background:#0b2a17;color:#fff;display:grid;place-items:center;min-height:100vh;margin:0}.card{text-align:center;padding:32px;border-radius:24px;background:#14532d;box-shadow:0 8px 0 #071b0f}#google{display:flex;justify-content:center;margin-top:24px}</style>
                </head><body><main class="card"><h1>EnglishAI.uz</h1><div id="google"></div></main>
                <script>
                const clientId=__CLIENT_ID__, state=__STATE__;
                window.onload=()=>{google.accounts.id.initialize({client_id:clientId,callback:async r=>{try{const x=await fetch('/api/auth/mobile-google/exchange',{method:'POST',headers:{'Content-Type':'application/json'},body:JSON.stringify({idToken:r.credential,state})});if(!x.ok)throw 0;const d=await x.json();location.replace(d.callbackUrl)}catch{location.replace('uz.englishai.app://auth/google?error=exchange_failed&state='+encodeURIComponent(state))}},auto_select:false,cancel_on_tap_outside:false});google.accounts.id.renderButton(document.getElementById('google'),{theme:'outline',size:'large',shape:'pill',text:'continue_with'});google.accounts.id.prompt()};
                </script></body></html>
                """
                .Replace("__CLIENT_ID__", clientId, StringComparison.Ordinal)
                .Replace("__STATE__", safeState, StringComparison.Ordinal);
            return Results.Content(html, "text/html; charset=utf-8");
        }).AllowAnonymous();

        group.MapPost("/mobile-google/exchange", async (MobileGoogleExchangeRequest request, MobileGoogleSignInStore store, CancellationToken cancellationToken) =>
        {
            if (string.IsNullOrWhiteSpace(request.IdToken) || string.IsNullOrWhiteSpace(request.State) || request.State.Length > 200)
                return Results.BadRequest();
            var code = await store.PutAsync(request.State, request.IdToken, cancellationToken);
            var callback = $"uz.englishai.app://auth/google?code={Uri.EscapeDataString(code)}&state={Uri.EscapeDataString(request.State)}";
            return Results.Ok(new MobileGoogleExchangeResponse(callback));
        }).AllowAnonymous();

        group.MapPost("/mobile-google/redeem", async (MobileGoogleRedeemRequest request, MobileGoogleSignInStore store, CancellationToken cancellationToken) =>
        {
            var idToken = await store.RedeemAsync(request.Code, request.State, cancellationToken);
            return idToken is null ? Results.BadRequest() : Results.Ok(new MobileGoogleRedeemResponse(idToken));
        }).AllowAnonymous();

        // Sign in / register with a Google ID token. Sets the session cookie (used by the
        // browser, which can't read HttpOnly cookies but sends them automatically) AND returns
        // the token in the body. The web SPA ignores the token and rides the cookie; the native
        // mobile shell (Capacitor) - where cross-origin cookies are unreliable - stores the token
        // and sends it as an Authorization: Bearer header instead. Both paths share this endpoint.
        group.MapPost("/google", async (
            GoogleSignInRequest request,
            HttpContext http,
            ISender sender,
            JwtOptions jwt) =>
        {
            var result = await sender.Send(new AuthenticateWithGoogleCommand(request.IdToken, request.ReferralCode));
            AppendSessionCookie(http, jwt, result.Token, result.ExpiresAt);
            return Results.Ok(new GoogleSignInResponse(result.User, result.Token, result.ExpiresAt));
        }).AllowAnonymous(); // the entry point - no session exists yet

        // Dev-only sign-in bypass (local `dotnet run`). Lets the SPA drive the whole app without
        // a Google Cloud project. Gated to the Development environment so it NEVER registers in
        // Production; the SPA only reveals the button under its own dev build. See
        // Application/Identity/DevLogin for the handler.
        if (app.ServiceProvider.GetRequiredService<IWebHostEnvironment>().IsDevelopment())
        {
            group.MapPost("/dev-login", async (
                HttpContext http,
                ISender sender,
                JwtOptions jwt) =>
            {
                var result = await sender.Send(new DevLoginCommand());
                AppendSessionCookie(http, jwt, result.Token, result.ExpiresAt);
                return Results.Ok(new GoogleSignInResponse(result.User, result.Token, result.ExpiresAt));
            }).AllowAnonymous().WithTags("Auth (dev)");
        }

        // Clear the session cookie. Anonymous so it always succeeds, even once the token expired.
        group.MapPost("/logout", (HttpContext http, JwtOptions jwt) =>
        {
            http.Response.Cookies.Delete(jwt.CookieName, CookieOptions(http));
            return Results.NoContent();
        }).AllowAnonymous();

        // The currently signed-in user, resolved from the validated session cookie.
        group.MapGet("/me", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetCurrentUserQuery(userId.Value)));
        }).RequireAuthorization();

        // Live username availability for the handle-setup and profile-edit screens. Excludes
        // the caller so changing nothing about their own handle never reports it as taken.
        group.MapGet("/username-available", async (string username, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new CheckUsernameAvailabilityQuery(username, userId.Value)));
        }).RequireAuthorization();

        // Update the editable profile fields (display name + username). The user id comes from
        // the session, never the body, so a caller can only ever edit their own profile.
        group.MapPut("/profile", async (UpdateProfileRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new UpdateProfileCommand(userId.Value, request.DisplayName, request.Username)));
        }).RequireAuthorization();

        group.MapPut("/demographics", async (SetDemographicsRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new SetDemographicsCommand(
                userId.Value,
                request.BirthDate,
                request.Gender,
                request.AcquisitionSource,
                request.AcquisitionSourceOther)));
        }).RequireAuthorization();

        group.MapPut("/avatar", async (IFormFile file, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();
            if (file.Length == 0 || file.Length > UpdateAvatarCommandValidator.MaxBytes)
                return Results.BadRequest(new { message = "Avatar image must be between 1 byte and 5 MB." });
            if (file.ContentType is not ("image/jpeg" or "image/png" or "image/webp"))
                return Results.BadRequest(new { message = "Avatar must be a JPEG, PNG, or WebP image." });

            try
            {
                await using var stream = file.OpenReadStream();
                var data = AvatarImageProcessor.ToSquareWebp(stream);
                return Results.Ok(await sender.Send(
                    new UpdateAvatarCommand(userId.Value, data, "image/webp")));
            }
            catch (InvalidDataException)
            {
                return Results.BadRequest(new { message = "The selected file is not a valid image." });
            }
        }).DisableAntiforgery().RequireAuthorization();

        group.MapDelete("/avatar", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();
            return Results.Ok(await sender.Send(new DeleteAvatarCommand(userId.Value)));
        }).RequireAuthorization();

        group.MapGet("/avatar/{userId:guid}", async (
            Guid userId, IUserAvatarStore avatars, IObjectStorage storage,
            ObjectStorageOptions storageOptions, HttpContext http, CancellationToken cancellationToken) =>
        {
            var avatar = await avatars.GetAsync(userId, cancellationToken);
            if (avatar is null) return Results.NotFound();
            if (!string.IsNullOrWhiteSpace(avatar.ObjectKey))
            {
                http.Response.Headers.CacheControl = "private, no-store";
                var url = await storage.CreatePrivateReadUrlAsync(avatar.ObjectKey,
                    TimeSpan.FromMinutes(Math.Clamp(storageOptions.PrivateReadUrlMinutes, 1, 30)), cancellationToken);
                return Results.Redirect(url.ToString());
            }
            return Results.File(avatar.Data!, avatar.ContentType, lastModified: avatar.UpdatedAt,
                entityTag: new Microsoft.Net.Http.Headers.EntityTagHeaderValue($"\"{avatar.UpdatedAt.ToUnixTimeMilliseconds()}\""));
        }).AllowAnonymous();

        // Stores the name the AI tutor addresses the learner by (the one-time "what should I
        // call you?" answer). The user id comes from the session, never the body.
        group.MapPut("/preferred-name", async (SetPreferredNameRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new SetPreferredNameCommand(userId.Value, request.PreferredName)));
        }).RequireAuthorization();

        // Stores the learner's onboarding goal (goal-based onboarding). The user id comes from the
        // session, never the body. Returns the refreshed user so the SPA can leave the goal gate.
        group.MapPut("/learning-goal", async (SetLearningGoalRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(
                new SetLearningGoalCommand(userId.Value, request.Goal)));
        }).RequireAuthorization();

        // Account preferences (settings) - read and save.
        group.MapGet("/preferences", async (HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new GetUserPreferencesQuery(userId.Value)));
        }).RequireAuthorization();

        group.MapPut("/preferences", async (UpdatePreferencesRequest request, HttpContext http, ISender sender) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            return Results.Ok(await sender.Send(new UpdateUserPreferencesCommand(
                userId.Value,
                request.DailyGoal,
                request.LanguageBalance,
                request.EmailNotifications,
                request.PushNotifications)));
        }).RequireAuthorization();

        // Permanently delete the signed-in user's account and all their data. The user id comes
        // from the session; the re-typed email is re-verified server-side before anything is
        // erased. On success the session cookie is dropped so the now-stale token can't be reused.
        group.MapDelete("/account", async (
            [FromBody] DeleteAccountRequest request, HttpContext http, ISender sender, JwtOptions jwt) =>
        {
            var userId = ResolveUserId(http.User);
            if (userId is null)
                return Results.Unauthorized();

            await sender.Send(new DeleteAccountCommand(userId.Value, request.ConfirmationEmail));
            http.Response.Cookies.Delete(jwt.CookieName, CookieOptions(http));
            return Results.NoContent();
        }).RequireAuthorization();

        return app;
    }

    private static void AppendSessionCookie(
        HttpContext http, JwtOptions jwt, string token, DateTimeOffset expiresAt)
    {
        var options = CookieOptions(http);
        options.Expires = expiresAt;
        http.Response.Cookies.Append(jwt.CookieName, token, options);
    }

    // HttpOnly so JS can never read the token; SameSite=Lax suits a same-origin SPA;
    // Secure follows the request scheme so it still works over http in local dev (rule 13).
    private static CookieOptions CookieOptions(HttpContext http) => new()
    {
        HttpOnly = true,
        Secure = http.Request.IsHttps,
        SameSite = SameSiteMode.Lax,
        Path = "/",
    };

    private static Guid? ResolveUserId(ClaimsPrincipal user)
    {
        var raw = user.FindFirst(JwtRegisteredClaimNames.Sub)?.Value
                  ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        return Guid.TryParse(raw, out var id) ? id : null;
    }

    public sealed record GoogleSignInRequest(string IdToken, string? ReferralCode = null);

    public sealed record AuthConfigResponse(string GoogleClientId);

    public sealed record MobileGoogleExchangeRequest(string IdToken, string State);
    public sealed record MobileGoogleExchangeResponse(string CallbackUrl);
    public sealed record MobileGoogleRedeemRequest(string Code, string State);
    public sealed record MobileGoogleRedeemResponse(string IdToken);

    // The web client reads only `User` (its session is the cookie); native clients also read
    // `Token`/`ExpiresAt` to drive Authorization-header auth where cookies don't carry over.
    public sealed record GoogleSignInResponse(
        AuthenticatedUserDto User,
        string Token,
        DateTimeOffset ExpiresAt);

    public sealed record UpdateProfileRequest(string DisplayName, string Username);
    public sealed record SetDemographicsRequest(
        DateOnly BirthDate,
        Gender Gender,
        AcquisitionSource AcquisitionSource,
        string? AcquisitionSourceOther);

    public sealed record SetPreferredNameRequest(string PreferredName);

    public sealed record SetLearningGoalRequest(Domain.Common.LearningGoal Goal);

    public sealed record DeleteAccountRequest(string ConfirmationEmail);

    public sealed record UpdatePreferencesRequest(
        DailyGoal DailyGoal,
        LanguageBalance LanguageBalance,
        bool EmailNotifications,
        bool PushNotifications);
}
