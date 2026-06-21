using System.Text.Json;
using Application.Video.Models;

namespace Infrastructure.Video;

/// <summary>
/// Pure, defensive parser for the LLM's transcript-translation JSON. Kept separate from the
/// network adapter so the validation pass (docs/development-guide.md rule 11 - second check on dynamic Uzbek)
/// is unit-testable with fixtures. Tolerant of code fences and surrounding prose; any malformed
/// payload yields <see cref="TranscriptTranslation.Empty"/> rather than throwing or guessing,
/// so a bad response simply leaves the transcript untranslated (honest "pending", rules 8, 11).
/// </summary>
public static class TranscriptTranslationParser
{
    private const int MaxGlossaryEntries = 80;
    private const int MaxGlossaryWordLength = 40;
    private const int MaxMeaningLength = 120;

    /// <summary>
    /// Parses one batch response into a translation aligned to <paramref name="expectedLineCount"/>
    /// lines (shorter arrays are padded with <c>null</c>, longer ones truncated).
    /// </summary>
    public static TranscriptTranslation Parse(string? modelText, int expectedLineCount)
    {
        var json = ExtractJsonObject(modelText);
        if (json is null)
            return TranscriptTranslation.Empty;

        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;

            var lines = ParseLines(root, expectedLineCount);
            var glossary = ParseGlossary(root);

            return new TranscriptTranslation(lines, glossary);
        }
        catch (JsonException)
        {
            return TranscriptTranslation.Empty;
        }
    }

    private static IReadOnlyList<string?> ParseLines(JsonElement root, int expectedLineCount)
    {
        var result = new string?[expectedLineCount];

        if (root.TryGetProperty("lines", out var lines) && lines.ValueKind == JsonValueKind.Array)
        {
            var i = 0;
            foreach (var item in lines.EnumerateArray())
            {
                if (i >= expectedLineCount)
                    break;
                var text = item.ValueKind == JsonValueKind.String ? item.GetString() : null;
                result[i] = string.IsNullOrWhiteSpace(text) ? null : text!.Trim();
                i++;
            }
        }

        return result;
    }

    private static IReadOnlyList<WordGloss> ParseGlossary(JsonElement root)
    {
        if (!root.TryGetProperty("glossary", out var glossary) || glossary.ValueKind != JsonValueKind.Array)
            return Array.Empty<WordGloss>();

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<WordGloss>();

        foreach (var item in glossary.EnumerateArray())
        {
            if (result.Count >= MaxGlossaryEntries)
                break;
            if (item.ValueKind != JsonValueKind.Object)
                continue;

            var word = ReadString(item, "w") ?? ReadString(item, "word");
            var meaning = ReadString(item, "uz") ?? ReadString(item, "meaning");
            if (word is null || meaning is null)
                continue;

            word = word.Trim();
            meaning = meaning.Trim();
            if (word.Length == 0 || word.Length > MaxGlossaryWordLength || meaning.Length == 0)
                continue;
            if (meaning.Length > MaxMeaningLength)
                meaning = meaning[..MaxMeaningLength];
            if (!seen.Add(word))
                continue;

            result.Add(new WordGloss(word, meaning));
        }

        return result;
    }

    private static string? ReadString(JsonElement obj, string name) =>
        obj.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;

    /// <summary>Pulls the first balanced JSON object out of the model text (drops fences/prose).</summary>
    private static string? ExtractJsonObject(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return null;

        var start = text.IndexOf('{');
        var end = text.LastIndexOf('}');
        return start >= 0 && end > start ? text[start..(end + 1)] : null;
    }
}
