using Domain.Common;
using Domain.Developer;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Developer;

public sealed class DeveloperApiKeyTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 21, 10, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_stores_hash_metadata_without_plaintext()
    {
        var userId = Guid.NewGuid();

        var key = DeveloperApiKey.Create(userId, "Mobile app", "eai_12345678", new string('A', 64), Now);

        key.UserId.Should().Be(userId);
        key.Name.Should().Be("Mobile app");
        key.IsActive.Should().BeTrue();
        key.LastUsedAt.Should().BeNull();
    }

    [Fact]
    public void Revoked_key_cannot_record_usage()
    {
        var key = DeveloperApiKey.Create(Guid.NewGuid(), "CLI", "eai_12345678", new string('B', 64), Now);
        key.Revoke(Now.AddMinutes(1));

        var act = () => key.RecordUsage(Now.AddMinutes(2));

        act.Should().Throw<DomainException>();
        key.IsActive.Should().BeFalse();
    }
}
