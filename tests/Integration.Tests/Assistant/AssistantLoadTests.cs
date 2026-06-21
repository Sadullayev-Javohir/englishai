using System.Net;
using System.Net.Http.Json;
using Application.Assistant.Ports;
using FluentAssertions;
using Infrastructure.Assistant;
using Infrastructure.Video;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace Integration.Tests.Assistant;

public sealed class AssistantLoadTests
{
    [Fact]
    public async Task Five_hundred_fake_users_share_one_identical_contextual_request()
    {
        var fake = new CountingContextualAssistant();
        using var factory = new TestWebApplicationFactory().WithWebHostBuilder(builder =>
            builder.ConfigureTestServices(services =>
            {
                services.RemoveAll<IContextualAssistant>();
                services.AddSingleton<IContextualAssistant>(fake);
                services.RemoveAll<LocalContextualAssistant>();
                services.AddSingleton<LocalContextualAssistant>();
                services.RemoveAll<ContextualAssistantOptions>();
                services.AddSingleton(new ContextualAssistantOptions());
                services.RemoveAll<IContextualAssistantCoordinator>();
                services.AddSingleton<IContextualAssistantCoordinator, ContextualAssistantCoordinator>();
                services.RemoveAll<Application.Video.Ports.IVideoExplainCache>();
                services.AddSingleton<Application.Video.Ports.IVideoExplainCache, InMemoryVideoExplainCache>();
            }));
        using var client = factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
        });

        var requests = Enumerable.Range(0, 500).Select(async _ =>
        {
            using var response = await client.PostAsJsonAsync("/api/assistant/context/stream", new
            {
                area = "grammar",
                title = "Present Perfect",
                context = "Subject + have/has + V3",
                focusText = "I have finished my homework.",
                question = "Bu qoidani tushuntiring",
                history = Array.Empty<object>(),
            });
            var body = await response.Content.ReadAsStringAsync();
            return (response.StatusCode, body);
        });

        var results = await Task.WhenAll(requests);

        results.Should().OnlyContain(result => result.StatusCode == HttpStatusCode.OK);
        results.Should().OnlyContain(result => result.body.Contains("event: chunk"));
        results.Should().OnlyContain(result => result.body.Contains("event: done"));
        fake.CallCount.Should().BeLessThanOrEqualTo(2,
            "identical concurrent questions should be coalesced; a late second wave may start after the first in-flight entry completes");
    }

    private sealed class CountingContextualAssistant : IContextualAssistant
    {
        private int _callCount;
        public int CallCount => Volatile.Read(ref _callCount);

        public async Task<string?> AnswerAsync(
            string area,
            string title,
            string context,
            string focusText,
            string question,
            IReadOnlyList<ContextualAssistantTurn> history,
            CancellationToken cancellationToken = default)
        {
            Interlocked.Increment(ref _callCount);
            await Task.Delay(100, cancellationToken);
            return "Present Perfect have yoki has va V3 bilan tuziladi.";
        }
    }
}
