using Application.Books.Models;
using Domain.Assessment;

namespace Application.Books.Ports;

/// <summary>
/// Generates the content of one book section: a cohesive, CEFR-leveled English passage that
/// continues the book's story, plus exactly ten comprehension questions with English explanations.
/// This is the sanctioned dynamic path of docs/development-guide.md rule 11 - the passage, questions and
/// explanations are the target language (English teaching content). Cost is controlled (rule 10)
/// by a budget model, a cached system prompt and a capped output. Implementations must return
/// <see cref="GeneratedBookSection.Empty"/> (never throw, never fabricate) when generation is
/// unavailable, so the section stays honestly "pending".
/// </summary>
public interface IBookContentGenerator
{
    Task<GeneratedBookSection> GenerateAsync(
        string bookTitle,
        string synopsis,
        string sectionTitle,
        int sectionNumber,
        int totalSections,
        CefrLevel level,
        CancellationToken cancellationToken = default);
}
