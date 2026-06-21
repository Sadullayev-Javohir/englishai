using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Xunit;

namespace Integration.Tests.Assistant;

public sealed class AssistantSessionFlowTests
{
    [Fact]
    public async Task Session_create_message_list_open_delete_flow_is_durable_for_the_host()
    {
        using var factory = new TestWebApplicationFactory();
        using var client = factory.CreateClient();
        (await client.PostAsync("/api/auth/dev-login", null)).EnsureSuccessStatusCode();

        var create = await client.PostAsJsonAsync("/api/assistant/sessions", new
        {
            skill = "grammar", resourceType = "topic", resourceId = "topic-1", title = "Present Perfect"
        });
        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var session = await create.Content.ReadFromJsonAsync<SessionDto>();
        session.Should().NotBeNull();
        session!.ExpiresAt.Should().BeAfter(DateTimeOffset.UtcNow.AddHours(23));

        var requestId = Guid.NewGuid();
        var first = await client.PostAsJsonAsync($"/api/assistant/sessions/{session.Id}/messages/stream", new
        { question = "Tushuntiring", focusText = "have + V3", clientRequestId = requestId });
        first.EnsureSuccessStatusCode();
        var firstBody = await first.Content.ReadAsStringAsync();
        firstBody.Should().Contain("event: done").And.NotContain("unavailable");

        var retry = await client.PostAsJsonAsync($"/api/assistant/sessions/{session.Id}/messages/stream", new
        { question = "Tushuntiring", focusText = "have + V3", clientRequestId = requestId });
        retry.EnsureSuccessStatusCode();

        var opened = await client.GetFromJsonAsync<SessionDto>($"/api/assistant/sessions/{session.Id}");
        opened!.Messages.Should().HaveCount(2, "retrying with the same clientRequestId must not duplicate messages");
        opened.ExpiresAt.Should().BeAfter(session.ExpiresAt);

        var listed = await client.GetFromJsonAsync<SessionPageDto>("/api/assistant/sessions");
        listed!.Items.Should().ContainSingle(x => x.Id == session.Id);

        (await client.DeleteAsync($"/api/assistant/sessions/{session.Id}")).StatusCode.Should().Be(HttpStatusCode.NoContent);
        (await client.GetAsync($"/api/assistant/sessions/{session.Id}")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    private sealed record MessageDto(Guid Id, string Role, string Text);
    private sealed record SessionDto(Guid Id, DateTimeOffset ExpiresAt, MessageDto[] Messages);
    private sealed record SessionPageDto(SessionDto[] Items, string? NextCursor);
}
