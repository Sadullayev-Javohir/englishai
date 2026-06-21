using Domain.Assistant;
using FluentAssertions;

namespace Domain.Tests.Assistant;

public sealed class AssistantSessionTests
{
    private static readonly Guid LearnerId = Guid.Parse("11111111-1111-1111-1111-111111111111");

    [Fact]
    public void Create_sets_twenty_four_hour_expiry()
    {
        var now = DateTimeOffset.Parse("2026-07-27T12:00:00Z");
        var session = AssistantSession.Create(LearnerId, "grammar", "topic", "topic-1", "Present perfect", now);
        session.ExpiresAt.Should().Be(now.AddHours(24));
    }

    [Fact]
    public void Message_refreshes_expiry_and_is_idempotent()
    {
        var now = DateTimeOffset.Parse("2026-07-27T12:00:00Z");
        var session = AssistantSession.Create(LearnerId, "grammar", "topic", "topic-1", "Present perfect", now);
        var requestId = Guid.NewGuid();
        var first = session.AddMessage("user", "Explain it", "completed", "local", requestId, null, now.AddHours(2));
        var duplicate = session.AddMessage("user", "Explain it", "completed", "local", requestId, null, now.AddHours(3));
        duplicate.Should().BeSameAs(first);
        session.Messages.Should().ContainSingle();
        session.ExpiresAt.Should().Be(now.AddHours(26));
    }
}
