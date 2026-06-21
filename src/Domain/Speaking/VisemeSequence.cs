using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// An ordered set of <see cref="VisemeFrame"/>s synchronized to a piece of
/// synthesized audio. Invariant: every frame falls within the audio duration and
/// frames are ordered by offset - this is what lets the frontend animate the mouth
/// in sync with playback (PROJECT-SPEC 17.1 requires a test proving this).
/// </summary>
public sealed class VisemeSequence
{
    private readonly List<VisemeFrame> _frames;

    private VisemeSequence(IReadOnlyList<VisemeFrame> frames, TimeSpan audioDuration, string? animation)
    {
        _frames = frames.OrderBy(f => f.AudioOffset).ToList();
        AudioDuration = audioDuration;
        Animation = animation;
    }

    public IReadOnlyList<VisemeFrame> Frames => _frames;
    public TimeSpan AudioDuration { get; }

    /// <summary>
    /// The complete mouth animation for the whole utterance, as a single self-contained SVG
    /// (Azure <c>redlips_front</c> delivers one SVG with internal SMIL timing for all visemes,
    /// not one SVG per frame). The renderer injects this once and plays it; null when the
    /// engine produced viseme ids only (no SVG was requested or the locale doesn't support it).
    /// </summary>
    public string? Animation { get; }

    public static VisemeSequence Create(
        IReadOnlyList<VisemeFrame> frames,
        TimeSpan audioDuration,
        string? animation = null)
    {
        if (audioDuration <= TimeSpan.Zero)
            throw new DomainException("Audio duration must be positive.");
        if (frames is null || frames.Count == 0)
            throw new DomainException("A viseme sequence requires at least one frame.");
        if (frames.Any(f => f.AudioOffset < TimeSpan.Zero || f.AudioOffset > audioDuration))
            throw new DomainException("Every viseme frame must fall within the audio duration.");

        return new VisemeSequence(frames, audioDuration, animation);
    }

    /// <summary>
    /// The frame that should be shown at the given playback offset (the latest frame
    /// whose offset is at or before <paramref name="offset"/>), or <c>null</c> if the
    /// offset precedes the first frame.
    /// </summary>
    public VisemeFrame? FrameAt(TimeSpan offset)
    {
        VisemeFrame? active = null;
        foreach (var frame in _frames)
        {
            if (frame.AudioOffset <= offset)
                active = frame;
            else
                break;
        }

        return active;
    }
}
