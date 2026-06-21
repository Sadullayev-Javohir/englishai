using Domain.Common;

namespace Domain.Books;

/// <summary>One learner's best result on a single book section.</summary>
public sealed class BookSectionScore
{
    // Parameterless ctor for EF Core materialization.
    private BookSectionScore()
    {
    }

    internal BookSectionScore(Guid sectionId, int correctCount, bool passed, DateTimeOffset achievedAt)
    {
        SectionId = sectionId;
        BestCorrectCount = correctCount;
        Passed = passed;
        AchievedAt = achievedAt;
    }

    public Guid SectionId { get; private set; }

    /// <summary>The learner's best (highest) number of correct answers on this section.</summary>
    public int BestCorrectCount { get; private set; }

    /// <summary>True once the learner has passed this section (at least 70%).</summary>
    public bool Passed { get; private set; }

    public DateTimeOffset AchievedAt { get; private set; }

    internal void Improve(int correctCount, bool passed, DateTimeOffset achievedAt)
    {
        // Keep the best result: a re-attempt can only raise the recorded score / confirm a pass.
        if (correctCount > BestCorrectCount)
            BestCorrectCount = correctCount;
        if (passed)
            Passed = true;
        AchievedAt = achievedAt;
    }
}

/// <summary>
/// A learner's reading progress through one <see cref="Book"/> - the set of sections they have
/// attempted, with the best result on each. A section counts as read once passed (at least 70%), and
/// the book is "read" (confirmed) once every section is passed. One row per (learner, book), the
/// same shape as a topic-completion record.
/// </summary>
public sealed class BookProgress
{
    private readonly List<BookSectionScore> _sections = new();

    // Parameterless ctor for EF Core materialization.
    private BookProgress()
    {
    }

    private BookProgress(Guid learnerId, Guid bookId, DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        LearnerId = learnerId;
        BookId = bookId;
        CreatedAt = now;
        UpdatedAt = now;
    }

    public Guid Id { get; private set; }

    public Guid LearnerId { get; private set; }

    public Guid BookId { get; private set; }

    /// <summary>Set once every section of the book has been passed.</summary>
    public DateTimeOffset? CompletedAt { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public DateTimeOffset UpdatedAt { get; private set; }

    public IReadOnlyList<BookSectionScore> Sections => _sections;

    /// <summary>Number of sections the learner has passed.</summary>
    public int PassedSectionCount => _sections.Count(s => s.Passed);

    /// <summary>True once the whole book has been confirmed read.</summary>
    public bool IsCompleted => CompletedAt is not null;

    public static BookProgress Start(Guid learnerId, Guid bookId, DateTimeOffset now)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("A book progress record needs a learner id.");
        if (bookId == Guid.Empty)
            throw new DomainException("A book progress record needs a book id.");

        return new BookProgress(learnerId, bookId, now);
    }

    /// <summary>True when the learner has passed the given section.</summary>
    public bool HasPassed(Guid sectionId) => _sections.Any(s => s.SectionId == sectionId && s.Passed);

    /// <summary>
    /// Records the outcome of a section quiz, keeping the best result per section. When this brings
    /// the number of passed sections up to <paramref name="totalSectionCount"/>, the book is marked
    /// completed. Idempotent: re-passing a section never un-completes the book.
    /// </summary>
    public void RecordSection(Guid sectionId, int correctCount, bool passed, int totalSectionCount, DateTimeOffset now)
    {
        if (sectionId == Guid.Empty)
            throw new DomainException("Section id must not be empty.");

        var existing = _sections.FirstOrDefault(s => s.SectionId == sectionId);
        if (existing is null)
            _sections.Add(new BookSectionScore(sectionId, correctCount, passed, now));
        else
            existing.Improve(correctCount, passed, now);

        UpdatedAt = now;

        if (CompletedAt is null && totalSectionCount > 0 && PassedSectionCount >= totalSectionCount)
            CompletedAt = now;
    }
}
