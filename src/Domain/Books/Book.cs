using Domain.Assessment;
using Domain.Common;

namespace Domain.Books;

/// <summary>
/// Aggregate root for a CEFR-leveled graded reader in the Books library (Home → Books). A book is
/// product content: its title, author and ordered section outline are curated/seeded, and each
/// section's English text plus ten comprehension questions are generated lazily on first open and
/// cached (docs/development-guide.md rules 8, 10). Learner-facing Uzbek text (<see cref="TitleUz"/>) is vetted
/// (rule 11). The cover image comes from a licensed source (Unsplash/Pexels) via the image
/// service and is persisted here once resolved, with its attribution (rule 12).
/// </summary>
public sealed class Book
{
    private readonly List<BookSection> _sections = new();

    // Parameterless ctor for EF Core materialization.
    private Book()
    {
        Title = null!;
        TitleUz = null!;
        Author = null!;
        Synopsis = null!;
        Topic = null!;
        CoverImageQuery = null!;
    }

    private Book(
        Guid id, string title, string titleUz, string author, string synopsis,
        string topic, CefrLevel level, string coverImageQuery, DateTimeOffset now)
    {
        Id = id;
        Title = title;
        TitleUz = titleUz;
        Author = author;
        Synopsis = synopsis;
        Topic = topic;
        Level = level;
        CoverImageQuery = coverImageQuery;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>The book title (English), shown in the library and reader.</summary>
    public string Title { get; private set; }

    /// <summary>A vetted Uzbek title/subtitle for the learner (rule 11).</summary>
    public string TitleUz { get; private set; }

    /// <summary>The author or "graded reader" attribution shown on the cover.</summary>
    public string Author { get; private set; }

    /// <summary>A short English synopsis shown on the book's detail page.</summary>
    public string Synopsis { get; private set; }

    /// <summary>Topic/genre tag used for curation and the cover-image query (e.g. "adventure").</summary>
    public string Topic { get; private set; }

    /// <summary>The curated CEFR level of the book.</summary>
    public CefrLevel Level { get; private set; }

    /// <summary>Keyword used to fetch a licensed cover image (Unsplash/Pexels), rule 12.</summary>
    public string CoverImageQuery { get; private set; }

    /// <summary>Resolved cover image URL, or null until the image service fetches one (lazy).</summary>
    public string? CoverImageUrl { get; private set; }

    /// <summary>Attribution for the cover image (photographer/source), stored for rule 12.</summary>
    public string? CoverAttribution { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<BookSection> Sections => _sections.OrderBy(s => s.Order).ToList();

    /// <summary>Total number of sections in the book.</summary>
    public int SectionCount => _sections.Count;

    /// <summary>True once a cover image has been resolved and persisted.</summary>
    public bool HasCover => !string.IsNullOrWhiteSpace(CoverImageUrl);

    /// <summary>
    /// Builds a curated book from its metadata and an ordered section outline (titles only - each
    /// section starts pending and is filled lazily on first open). Section orders are assigned
    /// 1-based in the supplied order.
    /// </summary>
    public static Book Curate(
        Guid id,
        string title,
        string titleUz,
        string author,
        string synopsis,
        string topic,
        CefrLevel level,
        string coverImageQuery,
        IEnumerable<string> sectionTitles,
        DateTimeOffset now)
    {
        if (id == Guid.Empty)
            throw new DomainException("A book needs a stable id.");
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Book title must not be empty.");
        if (string.IsNullOrWhiteSpace(titleUz))
            throw new DomainException("Book Uzbek title must not be empty.");
        if (string.IsNullOrWhiteSpace(author))
            throw new DomainException("Book author must not be empty.");
        if (string.IsNullOrWhiteSpace(synopsis))
            throw new DomainException("Book synopsis must not be empty.");
        if (string.IsNullOrWhiteSpace(topic))
            throw new DomainException("Book topic must not be empty.");
        if (string.IsNullOrWhiteSpace(coverImageQuery))
            throw new DomainException("Book cover image query must not be empty.");

        var titles = sectionTitles?.ToList() ?? throw new DomainException("Section titles must not be null.");
        if (titles.Count == 0)
            throw new DomainException("A book must have at least one section.");

        var book = new Book(
            id, title.Trim(), titleUz.Trim(), author.Trim(), synopsis.Trim(),
            topic.Trim(), level, coverImageQuery.Trim(), now);

        var order = 1;
        foreach (var sectionTitle in titles)
            book._sections.Add(BookSection.Pending(order++, sectionTitle));

        return book;
    }

    /// <summary>Returns the section with the given id, or null if it is not part of this book.</summary>
    public BookSection? FindSection(Guid sectionId) => _sections.FirstOrDefault(s => s.Id == sectionId);

    public void AdminUpdate(string title, string titleUz, string author, string synopsis, string topic,
        CefrLevel level, string coverImageQuery)
    {
        if (new[] { title, titleUz, author, synopsis, topic, coverImageQuery }.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Book metadata fields must not be empty.");
        Title = title.Trim(); TitleUz = titleUz.Trim(); Author = author.Trim(); Synopsis = synopsis.Trim();
        Topic = topic.Trim(); Level = level; CoverImageQuery = coverImageQuery.Trim();
    }

    /// <summary>Stores a resolved cover image URL and its attribution (called once, lazily).</summary>
    public void SetCover(string url, string? attribution)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new DomainException("Cover image url must not be empty.");

        CoverImageUrl = url.Trim();
        CoverAttribution = string.IsNullOrWhiteSpace(attribution) ? null : attribution.Trim();
    }
}
