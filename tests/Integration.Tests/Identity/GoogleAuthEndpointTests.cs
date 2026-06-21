using System.Net;
using System.Net.Http.Json;
using System.Net.Http.Headers;
using Application.Identity.Ports;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Formats.Png;
using Web.Endpoints;
using Xunit;

namespace Integration.Tests.Identity;

public sealed class GoogleAuthEndpointTests
{
    [Fact]
    public async Task Valid_google_identity_sets_secure_cookie_and_authenticates_me()
    {
        await using var factory = CreateFactory(new GoogleUserInfo(
            "google-subject-1",
            "learner@example.com",
            EmailVerified: true,
            "Test Learner",
            PictureUrl: null));
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/google",
            new AuthEndpoints.GoogleSignInRequest("valid-test-token"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var setCookie = response.Headers.GetValues("Set-Cookie").Single();
        setCookie.Should().Contain("englishai_auth=");
        setCookie.Should().Contain("httponly");
        setCookie.Should().Contain("secure");
        setCookie.Should().Contain("samesite=lax");
        setCookie.Should().Contain("path=/");

        var me = await client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Invalid_google_identity_is_rejected_without_a_session_cookie()
    {
        await using var factory = CreateFactory(identity: null);
        using var client = CreateHttpsClient(factory);

        var response = await client.PostAsJsonAsync(
            "/api/auth/google",
            new AuthEndpoints.GoogleSignInRequest("invalid-test-token"));

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Headers.TryGetValues("Set-Cookie", out _).Should().BeFalse();
    }

    [Fact]
    public async Task Signed_in_user_can_upload_and_fetch_avatar()
    {
        await using var factory = CreateFactory(new GoogleUserInfo(
            "google-subject-avatar",
            "avatar@example.com",
            EmailVerified: true,
            "Avatar Learner",
            PictureUrl: null));
        using var client = CreateHttpsClient(factory);
        await client.PostAsJsonAsync("/api/auth/google", new AuthEndpoints.GoogleSignInRequest("valid-test-token"));

        using var content = new MultipartFormDataContent();
        using var source = new Image<Rgba32>(2, 2, Color.LimeGreen);
        await using var imageStream = new MemoryStream();
        await source.SaveAsync(imageStream, new PngEncoder());
        var image = new ByteArrayContent(imageStream.ToArray());
        image.Headers.ContentType = new MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "avatar.png");

        var upload = await client.PutAsync("/api/auth/avatar", content);
        upload.StatusCode.Should().Be(HttpStatusCode.OK, await upload.Content.ReadAsStringAsync());
        var user = await upload.Content.ReadFromJsonAsync<Application.Identity.Dtos.AuthenticatedUserDto>();
        user!.PictureUrl.Should().StartWith("/api/auth/avatar/");

        var avatar = await client.GetAsync(user.PictureUrl);
        avatar.StatusCode.Should().Be(HttpStatusCode.OK);
        avatar.Content.Headers.ContentType!.MediaType.Should().Be("image/webp");
    }

    [Fact]
    public async Task Mobile_google_bridge_exchanges_and_redeems_a_one_time_code()
    {
        await using var factory = CreateFactory(identity: null);
        using var client = CreateHttpsClient(factory);

        var page = await client.GetAsync("/api/auth/mobile-google?state=state-123");
        page.StatusCode.Should().Be(HttpStatusCode.OK);
        var pageHtml = await page.Content.ReadAsStringAsync();
        pageHtml.Should().Contain("accounts.google.com/gsi/client");
        pageHtml.Should().Contain("google.accounts.id.prompt()");
        pageHtml.Should().NotContain("Davom etish uchun Google hisobingizni tanlang");

        var exchange = await client.PostAsJsonAsync(
            "/api/auth/mobile-google/exchange",
            new AuthEndpoints.MobileGoogleExchangeRequest("google-id-token", "state-123"));
        exchange.EnsureSuccessStatusCode();
        var handoff = await exchange.Content.ReadFromJsonAsync<AuthEndpoints.MobileGoogleExchangeResponse>();
        handoff.Should().NotBeNull();
        var callback = new Uri(handoff!.CallbackUrl);
        callback.Scheme.Should().Be("uz.englishai.app");
        var query = Microsoft.AspNetCore.WebUtilities.QueryHelpers.ParseQuery(callback.Query);

        var redeem = await client.PostAsJsonAsync(
            "/api/auth/mobile-google/redeem",
            new AuthEndpoints.MobileGoogleRedeemRequest(query["code"]!, query["state"]!));
        redeem.EnsureSuccessStatusCode();
        var token = await redeem.Content.ReadFromJsonAsync<AuthEndpoints.MobileGoogleRedeemResponse>();
        token!.IdToken.Should().Be("google-id-token");

        var replay = await client.PostAsJsonAsync(
            "/api/auth/mobile-google/redeem",
            new AuthEndpoints.MobileGoogleRedeemRequest(query["code"]!, query["state"]!));
        replay.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    private static Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> CreateFactory(
        GoogleUserInfo? identity) =>
        new TestWebApplicationFactory().WithWebHostBuilder(builder =>
        {
            builder.UseSetting("Auth:Jwt:SigningKey", "integration-test-signing-key-with-at-least-32-bytes");
            builder.UseSetting("Auth:Google:ClientId", "test-client.apps.googleusercontent.com");
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IGoogleTokenValidator>();
                services.AddSingleton<IGoogleTokenValidator>(new StubGoogleTokenValidator(identity));
            });
        });

    private static HttpClient CreateHttpsClient(
        Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactory<Program> factory) =>
        factory.CreateClient(new Microsoft.AspNetCore.Mvc.Testing.WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("https://localhost"),
            HandleCookies = true,
        });

    private sealed class StubGoogleTokenValidator(GoogleUserInfo? identity) : IGoogleTokenValidator
    {
        public Task<GoogleUserInfo?> ValidateAsync(string idToken, CancellationToken cancellationToken) =>
            Task.FromResult(idToken == "valid-test-token" ? identity : null);
    }
}
