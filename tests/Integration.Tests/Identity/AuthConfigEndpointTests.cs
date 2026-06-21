using System.Net.Http.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Web.Endpoints;
using Xunit;

namespace Integration.Tests.Identity;

public class AuthConfigEndpointTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly HttpClient _client;

    public AuthConfigEndpointTests(TestWebApplicationFactory factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
            builder.UseSetting("Auth:Google:ClientId", "test-client.apps.googleusercontent.com"))
            .CreateClient();
    }

    [Fact]
    public async Task Config_is_public_and_returns_the_google_client_id_shape()
    {
        var response = await _client.GetAsync("/api/auth/config");

        response.EnsureSuccessStatusCode();
        var config = await response.Content.ReadFromJsonAsync<AuthEndpoints.AuthConfigResponse>();
        config.Should().NotBeNull();
        config!.GoogleClientId.Should().Be("test-client.apps.googleusercontent.com");
    }
}
