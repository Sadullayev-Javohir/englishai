using Application.Books.Models;
using Application.Books.Ports;
using Domain.Assessment;

namespace Infrastructure.Books;

public sealed class FallbackBookContentGenerator : IBookContentGenerator
{
    private readonly IBookContentGenerator _primary;
    private readonly IBookContentGenerator _fallback;

    public FallbackBookContentGenerator(
        IBookContentGenerator primary,
        IBookContentGenerator fallback)
    {
        _primary = primary;
        _fallback = fallback;
    }

    public async Task<GeneratedBookSection> GenerateAsync(
        string bookTitle,
        string synopsis,
        string sectionTitle,
        int sectionNumber,
        int totalSections,
        CefrLevel level,
        CancellationToken cancellationToken = default)
    {
        GeneratedBookSection result;
        try
        {
            result = await _primary.GenerateAsync(
                bookTitle, synopsis, sectionTitle, sectionNumber, totalSections, level, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception)
        {
            result = GeneratedBookSection.Empty;
        }

        if (!result.HasContent)
        {
            return await _fallback.GenerateAsync(
                bookTitle, synopsis, sectionTitle, sectionNumber, totalSections, level, cancellationToken);
        }

        return result;
    }
}
