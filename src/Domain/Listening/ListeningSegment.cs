using Domain.Common;

namespace Domain.Listening;

public sealed class ListeningSegment
{
    private ListeningSegment()
    {
        Speaker = null!;
        Text = null!;
    }

    private ListeningSegment(int order, int startMs, int endMs, string speaker, string text)
    {
        Id = Guid.NewGuid();
        Order = order;
        StartMs = startMs;
        EndMs = endMs;
        Speaker = speaker;
        Text = text;
    }

    public Guid Id { get; private set; }
    public int Order { get; private set; }
    public int StartMs { get; private set; }
    public int EndMs { get; private set; }
    public string Speaker { get; private set; }
    public string Text { get; private set; }

    public static ListeningSegment Create(int order, int startMs, int endMs, string speaker, string text)
    {
        if (order < 0)
            throw new DomainException("Segment order must not be negative.");
        if (startMs < 0 || endMs <= startMs)
            throw new DomainException("Segment end must be after its non-negative start.");
        if (string.IsNullOrWhiteSpace(speaker))
            throw new DomainException("Segment speaker must not be empty.");
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("Segment text must not be empty.");

        return new ListeningSegment(order, startMs, endMs, speaker.Trim(), text.Trim());
    }
}
