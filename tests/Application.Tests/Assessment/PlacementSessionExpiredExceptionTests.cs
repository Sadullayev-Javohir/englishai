using Application.Assessment;
using FluentAssertions;

namespace Application.Tests.Assessment;

public sealed class PlacementSessionExpiredExceptionTests
{
    [Fact]
    public void Exposes_stable_machine_code()
    {
        PlacementSessionExpiredException.ErrorCode.Should().Be("placement_session_expired");
    }
}
