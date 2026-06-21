using System.Reflection;

namespace Infrastructure.Speaking;

/// <summary>
/// Loads the embedded CMU Pronouncing Dictionary (~135k English words) once and exposes
/// each word's ARPABET phoneme tokens. This gives the pronunciation-detail screen
/// coverage for essentially any real English word the learner says, instead of a small
/// curated list. Public domain content, cached locally (docs/development-guide.md rules 11–12).
/// </summary>
internal sealed class CmuPronunciationDictionary
{
    private const string ResourceSuffix = "cmudict.dict";
    private const string CommentMarker = "#";

    private static readonly Lazy<CmuPronunciationDictionary> LazyInstance =
        new(Load, isThreadSafe: true);

    private readonly IReadOnlyDictionary<string, string[]> _entries;

    private CmuPronunciationDictionary(IReadOnlyDictionary<string, string[]> entries)
    {
        _entries = entries;
    }

    public static CmuPronunciationDictionary Instance => LazyInstance.Value;

    /// <summary>Returns the ARPABET tokens for a word, or null if it is not in the dictionary.</summary>
    public string[]? GetArpabet(string word) =>
        _entries.GetValueOrDefault(word.Trim().ToLowerInvariant());

    private static CmuPronunciationDictionary Load()
    {
        var assembly = typeof(CmuPronunciationDictionary).Assembly;
        var resourceName = assembly.GetManifestResourceNames()
            .FirstOrDefault(n => n.EndsWith(ResourceSuffix, StringComparison.Ordinal))
            ?? throw new InvalidOperationException($"Embedded resource '{ResourceSuffix}' was not found.");

        using var stream = assembly.GetManifestResourceStream(resourceName)!;
        using var reader = new StreamReader(stream);

        // Only keep the first (primary) pronunciation of each word; ignore "word(2)" alternates.
        var entries = new Dictionary<string, string[]>(StringComparer.Ordinal);

        string? line;
        while ((line = reader.ReadLine()) is not null)
        {
            var content = StripComment(line).Trim();
            if (content.Length == 0)
                continue;

            var firstSpace = content.IndexOf(' ');
            if (firstSpace <= 0)
                continue;

            var word = content[..firstSpace];
            if (word.EndsWith(')'))
                continue; // alternate pronunciation, e.g. "read(2)"

            var tokens = content[(firstSpace + 1)..]
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (tokens.Length == 0)
                continue;

            entries.TryAdd(word, tokens);
        }

        return new CmuPronunciationDictionary(entries);
    }

    private static string StripComment(string line)
    {
        var hash = line.IndexOf(CommentMarker, StringComparison.Ordinal);
        return hash >= 0 ? line[..hash] : line;
    }
}
