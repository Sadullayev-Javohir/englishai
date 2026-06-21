namespace Domain.Reading;

/// <summary>
/// Lifecycle of a topic-scoped reading lesson: <see cref="Pending"/> until its body, glossary and
/// comprehension questions have been generated and cached, then <see cref="Filled"/>. Legacy
/// stand-alone curated passages are created already <see cref="Filled"/>.
/// </summary>
public enum ReadingPassageStatus
{
    Pending = 0,
    Filled = 1,
}
