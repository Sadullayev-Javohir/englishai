using FluentAssertions;
using Web.Hubs;
using Xunit;

namespace Integration.Tests.Speaking;

public sealed class SpeakingLiveTurnRegistryTests
{
    [Fact]
    public void Claim_prevents_duplicate_processing_and_remembers_completion()
    {
        var registry = new SpeakingLiveTurnRegistry();
        var turnId = Guid.NewGuid();

        registry.Claim(turnId, 32).Should().Be(TurnClaim.Acquired);
        registry.Claim(turnId, 32).Should().Be(TurnClaim.Processing);

        registry.Complete(turnId);

        registry.Claim(turnId, 32).Should().Be(TurnClaim.Completed);
    }

    [Fact]
    public void Release_allows_cancelled_turn_to_retry()
    {
        var registry = new SpeakingLiveTurnRegistry();
        var turnId = Guid.NewGuid();

        registry.Claim(turnId, 32).Should().Be(TurnClaim.Acquired);
        registry.Release(turnId);

        registry.Claim(turnId, 32).Should().Be(TurnClaim.Acquired);
    }
}
