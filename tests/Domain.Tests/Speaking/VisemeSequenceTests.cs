using Domain.Common;
using Domain.Speaking;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Speaking;

public class VisemeSequenceTests
{
    private static VisemeFrame Frame(int id, int ms) => new(id, TimeSpan.FromMilliseconds(ms));

    [Fact]
    public void Create_orders_frames_by_offset()
    {
        var sequence = VisemeSequence.Create(
            new[] { Frame(3, 200), Frame(1, 0), Frame(2, 100) },
            TimeSpan.FromMilliseconds(300));

        sequence.Frames.Select(f => f.VisemeId).Should().Equal(1, 2, 3);
    }

    // PROJECT-SPEC 17.1: the viseme sequence must stay in sync with the audio length.
    [Fact]
    public void Every_frame_falls_within_the_audio_duration()
    {
        var duration = TimeSpan.FromMilliseconds(500);
        var sequence = VisemeSequence.Create(
            new[] { Frame(1, 0), Frame(2, 250), Frame(3, 500) },
            duration);

        sequence.Frames.Should().OnlyContain(f => f.AudioOffset >= TimeSpan.Zero && f.AudioOffset <= duration);
        sequence.AudioDuration.Should().Be(duration);
    }

    [Fact]
    public void Create_rejects_a_frame_beyond_the_audio_duration()
    {
        var act = () => VisemeSequence.Create(
            new[] { Frame(1, 0), Frame(2, 600) },
            TimeSpan.FromMilliseconds(500));

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_empty_frames()
    {
        var act = () => VisemeSequence.Create(Array.Empty<VisemeFrame>(), TimeSpan.FromSeconds(1));
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_non_positive_duration()
    {
        var act = () => VisemeSequence.Create(new[] { Frame(1, 0) }, TimeSpan.Zero);
        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void FrameAt_returns_the_active_frame_for_an_offset()
    {
        var sequence = VisemeSequence.Create(
            new[] { Frame(1, 0), Frame(2, 100), Frame(3, 200) },
            TimeSpan.FromMilliseconds(300));

        sequence.FrameAt(TimeSpan.FromMilliseconds(150))!.VisemeId.Should().Be(2);
        sequence.FrameAt(TimeSpan.FromMilliseconds(200))!.VisemeId.Should().Be(3);
    }

    [Fact]
    public void FrameAt_returns_null_before_the_first_frame()
    {
        var sequence = VisemeSequence.Create(
            new[] { Frame(5, 50) },
            TimeSpan.FromMilliseconds(100));

        sequence.FrameAt(TimeSpan.FromMilliseconds(10)).Should().BeNull();
    }
}
