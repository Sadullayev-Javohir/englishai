using System.Security.Cryptography;
using System.Text;
using Domain.Vocabulary;

namespace Application.Common;

/// <summary>One resolved image: its URL and an optional attribution string (rule 12).</summary>
public sealed record ImageResult(string Url, string? Attribution);

public static class WordImageQuery
{
    public static string From(
        string word,
        string translation,
        PartOfSpeech partOfSpeech = PartOfSpeech.Other,
        string? exampleSentence = null)
    {
        var value = word.Trim();
        if (value.Length == 0)
            value = translation.Trim();

        var tokens = value.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length > 1)
        {
            var head = tokens.LastOrDefault(token => token.Length > 2 && !IsStopword(token))
                       ?? tokens[^1];
            value = head;
        }

        value = value.TrimEnd('.', ',', '!', '?', '"', '\'');
        return partOfSpeech switch
        {
            PartOfSpeech.Verb or PartOfSpeech.PhrasalVerb => $"person {value} action",
            PartOfSpeech.Adjective when !string.IsNullOrWhiteSpace(exampleSentence) =>
                $"{value} {SemanticContext(exampleSentence)}",
            PartOfSpeech.Adverb => $"person action {value}",
            _ => value,
        };
    }

    public static Guid ImageId(Guid topicId, string word)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes($"{topicId:N}:{word.Trim().ToLowerInvariant()}"));
        return new Guid(bytes.AsSpan(0, 16));
    }

    private static string SemanticContext(string sentence)
    {
        var context = sentence.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Select(token => token.Trim('.', ',', '!', '?', '"', '\''))
            .FirstOrDefault(token => token.Length > 3 && !IsStopword(token));
        return context ?? "concept";
    }

    private static bool IsStopword(string token) =>
        token.Equals("the", StringComparison.OrdinalIgnoreCase)
        || token.Equals("a", StringComparison.OrdinalIgnoreCase)
        || token.Equals("an", StringComparison.OrdinalIgnoreCase)
        || token.Equals("of", StringComparison.OrdinalIgnoreCase)
        || token.Equals("out", StringComparison.OrdinalIgnoreCase)
        || token.Equals("up", StringComparison.OrdinalIgnoreCase)
        || token.Equals("in", StringComparison.OrdinalIgnoreCase)
        || token.Equals("on", StringComparison.OrdinalIgnoreCase);
}

/// <summary>
/// One downloaded image: its raw bytes plus the provenance needed to store and attribute it
/// (docs/development-guide.md rule 12). Unlike <see cref="ImageResult"/> (a hot-linked URL), this carries the
/// actual content so it can be persisted in the database and re-served from there.
/// </summary>
public sealed record DownloadedImage(
    byte[] Data,
    string ContentType,
    string Source,
    string? Attribution,
    string? SourceUrl,
    int Width,
    int Height,
    ImageSafetyStatus SafetyStatus = ImageSafetyStatus.Pending,
    string? SafetyModelVersion = null,
    DateTimeOffset? SafetyCheckedAt = null,
    string? SafetyReasons = null);

/// <summary>
/// Port for fetching a licensed image by keyword (docs/development-guide.md rule 12 - never a random or
/// copyrighted image; only licensed/open/generated sources, with attribution preserved).
/// Implemented by an Unsplash/Pexels adapter when an access key is configured, and by a local
/// no-op adapter otherwise (returns null so callers fall back to a generated placeholder cover).
/// Resolved URLs are persisted by the caller so the same image is reused (no per-request refetch).
/// </summary>
public interface IImageService
{
    /// <summary>
    /// Resolves a licensed image for <paramref name="query"/>, or null when none is available
    /// (no key configured, lookup failed, or no match) - callers must degrade gracefully.
    /// </summary>
    Task<ImageResult?> FindImageAsync(string query, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves a licensed image for <paramref name="query"/> and downloads its bytes so the
    /// caller can persist it in the database (docs/development-guide.md rule 12 - fetch once, store, reuse;
    /// never hot-link a copyrighted source). Returns null when none is available (no key, lookup
    /// or download failed, or no match) - callers must degrade gracefully to a placeholder.
    /// </summary>
    Task<DownloadedImage?> DownloadImageAsync(string query, CancellationToken cancellationToken);

    /// <summary>
    /// Resolves and downloads up to <paramref name="count"/> distinct licensed images for
    /// <paramref name="query"/> so the caller can build a topic's gallery (rule 12 - fetch once,
    /// store, reuse). Returns however many could be fetched (possibly fewer than requested, or
    /// empty when none is available) - callers must degrade gracefully. The first item, when
    /// present, is the best match (used as the cover).
    /// </summary>
    Task<IReadOnlyList<DownloadedImage>> DownloadImagesAsync(
        string query, int count, CancellationToken cancellationToken);
}
