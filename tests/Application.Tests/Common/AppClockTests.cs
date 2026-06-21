using Application.Common;
using Application.Tests.Learning;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Common;

/// <summary>
/// Locks in that all daily boundaries follow Uzbekistan local time (UTC+5), so the Home daily plan
/// and the Free-tier daily Speaking session roll over at local midnight (00:00 Tashkent), not UTC
/// midnight (05:00 local).
/// </summary>
public sealed class AppClockTests
{
    [Fact]
    public void LocalToday_uses_UTC_plus_5_so_the_day_flips_at_local_midnight()
    {
        // 2026-06-26 21:00 UTC is already 2026-06-27 02:00 in Tashkent - the local day has rolled over.
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 6, 26, 21, 0, 0, TimeSpan.Zero));

        clock.LocalToday().Should().Be(new DateOnly(2026, 6, 27));
    }

    [Fact]
    public void LocalToday_before_local_midnight_is_still_the_same_day()
    {
        // 2026-06-26 18:00 UTC is 2026-06-26 23:00 in Tashkent - still the same local day.
        var clock = new FixedTimeProvider(new DateTimeOffset(2026, 6, 26, 18, 0, 0, TimeSpan.Zero));

        clock.LocalToday().Should().Be(new DateOnly(2026, 6, 26));
    }

    [Fact]
    public void ToLocalDate_shifts_a_late_UTC_evening_into_the_next_local_day()
    {
        var moment = new DateTimeOffset(2026, 1, 1, 20, 30, 0, TimeSpan.Zero);

        moment.ToLocalDate().Should().Be(new DateOnly(2026, 1, 2));
    }
}
