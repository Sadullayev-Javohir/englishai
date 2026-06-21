namespace Domain.Books;

/// <summary>
/// Lifecycle of a book section: <see cref="Pending"/> until its English text and ten
/// comprehension questions have been generated and cached, then <see cref="Filled"/>. A book is
/// seeded as metadata (title, level, cover, section outline) with every section pending; each
/// section is filled lazily on first open (the same lazy-fill pattern as a reading lesson).
/// </summary>
public enum BookSectionStatus
{
    Pending = 0,
    Filled = 1,
}
