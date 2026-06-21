using Application.Common;
using FluentAssertions;

namespace Application.Tests.Common;

public sealed class StableCursorTests
{
    [Fact]
    public void Round_trips_timestamp_and_unique_id()
    {
        var expected = new StableCursor(DateTimeOffset.Parse("2026-07-30T10:00:00Z"), Guid.NewGuid());
        StableCursor.Decode(expected.Encode()).Should().Be(expected);
    }

    [Fact]
    public void Rejects_invalid_cursor()
    {
        var act = () => StableCursor.Decode("not-base64");
        act.Should().Throw<ArgumentException>().WithMessage("Invalid pagination cursor.*");
    }
}
