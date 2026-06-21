using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Application.Common;
using Application.Developer.Ports;
using Application.Identity.Dtos;
using Domain.Developer;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Xunit;

namespace Integration.Tests;

public sealed class DeveloperApiFlowTests : IClassFixture<TestWebApplicationFactory>
{
    private readonly TestWebApplicationFactory _factory;

    public DeveloperApiFlowTests(TestWebApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task Key_management_rejects_callers_without_a_super_admin_session()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/developer/keys");

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Key_management_forbids_an_authenticated_non_super_admin()
    {
        var admin = Substitute.For<IAdminAuthorization>();
        admin.GetRoleAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns(AdminRole.Admin);
        var application = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IAdminAuthorization>();
            services.AddScoped(_ => admin);
        }));
        var client = application.CreateClient();
        await client.PostAsync("/api/auth/dev-login", null);

        var response = await client.GetAsync("/api/developer/keys");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task Chat_completion_requires_a_valid_EnglishAi_key_and_forces_the_free_model()
    {
        var application = _factory.WithWebHostBuilder(builder => builder.ConfigureServices(services =>
        {
            services.RemoveAll<IDeveloperAiGateway>();
            services.AddSingleton<IDeveloperAiGateway, FakeGateway>();
        }));
        var client = application.CreateClient();

        var store = application.Services.GetRequiredService<IDeveloperApiKeyStore>();
        var protector = application.Services.GetRequiredService<IDeveloperApiKeyProtector>();
        const string plaintext = "eai_test_1234567890";
        await store.AddAsync(DeveloperApiKey.Create(
            Guid.NewGuid(), "Integration", plaintext[..DeveloperApiKey.PrefixLength], protector.Hash(plaintext),
            DateTimeOffset.UtcNow), CancellationToken.None);

        var unauthorized = await client.PostAsync("/v1/chat/completions",
            new StringContent("{\"messages\":[{\"role\":\"user\",\"content\":\"Hi\"}]}", Encoding.UTF8, "application/json"));
        unauthorized.StatusCode.Should().Be(HttpStatusCode.Unauthorized);

        using var request = new HttpRequestMessage(HttpMethod.Post, "/v1/chat/completions")
        {
            Content = new StringContent("{\"model\":\"paid/model\",\"messages\":[{\"role\":\"user\",\"content\":\"Hi\"}]}", Encoding.UTF8, "application/json")
        };
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", plaintext);
        var response = await client.SendAsync(request);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        json.RootElement.GetProperty("model").GetString().Should().Be("stepfun/step-3.7-flash:free");
    }

    private sealed class FakeGateway : IDeveloperAiGateway
    {
        public string Model => "stepfun/step-3.7-flash:free";

        public Task<DeveloperAiGatewayResponse> SendChatCompletionAsync(
            JsonElement request,
            CancellationToken cancellationToken) =>
            Task.FromResult(new DeveloperAiGatewayResponse(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{\"id\":\"chatcmpl-test\",\"model\":\"stepfun/step-3.7-flash:free\",\"choices\":[{\"message\":{\"role\":\"assistant\",\"content\":\"OK\"}}]}",
                    Encoding.UTF8,
                    "application/json")
            }));
    }
}
