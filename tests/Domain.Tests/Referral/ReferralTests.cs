using Domain.Referral;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Referral;

/// <summary>
/// Unit tests for the <see cref="Referral"/> link: creation guards (no self-referral) and the
/// once-only qualification transition that ensures the reward is paid exactly once.
/// </summary>
public class ReferralTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 1, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Create_records_a_pending_referral()
    {
        var referrer = Guid.NewGuid();
        var referee = Guid.NewGuid();

        var referral = Domain.Referral.Referral.Create(referrer, referee, "abc234", Now);

        referral.ReferrerId.Should().Be(referrer);
        referral.RefereeId.Should().Be(referee);
        referral.Status.Should().Be(ReferralStatus.Pending);
        referral.Code.Should().Be("ABC234"); // normalized to uppercase
        referral.QualifiedAt.Should().BeNull();
    }

    [Fact]
    public void Create_rejects_self_referral()
    {
        var same = Guid.NewGuid();

        var act = () => Domain.Referral.Referral.Create(same, same, ReferralCode.Generate(), Now);

        act.Should().Throw<Domain.Common.DomainException>();
    }

    [Fact]
    public void MarkQualified_transitions_once_and_is_idempotent()
    {
        var referral = Domain.Referral.Referral.Create(Guid.NewGuid(), Guid.NewGuid(), ReferralCode.Generate(), Now);

        referral.MarkQualified(Now).Should().BeTrue();
        referral.Status.Should().Be(ReferralStatus.Qualified);
        referral.QualifiedAt.Should().Be(Now);

        // A second call is a harmless no-op - the reward must never be granted twice.
        referral.MarkQualified(Now.AddDays(1)).Should().BeFalse();
        referral.QualifiedAt.Should().Be(Now);
    }
}
