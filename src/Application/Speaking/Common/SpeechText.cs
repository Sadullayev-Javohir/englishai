using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Application.Speaking.Common;

/// <summary>
/// Normalises a tutor reply so neither the on-screen bubble nor the text-to-speech
/// voice carry formatting noise. Markdown emphasis markers (** __ * _ ~~ `), heading
/// and quote markers, emojis, stickers and other pictographic symbols are stripped,
/// while the words and ordinary punctuation are kept. Applied once to the reply so the
/// displayed text and the spoken audio stay identical.
/// </summary>
public static partial class SpeechText
{
    public static string Clean(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;

        // 1) Markdown link [label](url) -> label, before the brackets/symbols are stripped.
        var s = MarkdownLink().Replace(text, "$1");

        // 2) Drop markdown emphasis and structure markers but keep the surrounding words.
        s = MarkdownMarkers().Replace(s, " ");

        // 3) Remove emojis, stickers and other symbol/format code points rune by rune so
        //    the TTS voice never tries to read them aloud (e.g. "😀", "🎉", "⭐").
        var sb = new StringBuilder(s.Length);
        foreach (var rune in s.EnumerateRunes())
        {
            var category = Rune.GetUnicodeCategory(rune);
            switch (category)
            {
                case UnicodeCategory.OtherSymbol:
                case UnicodeCategory.ModifierSymbol:
                case UnicodeCategory.Format:
                case UnicodeCategory.PrivateUse:
                case UnicodeCategory.Surrogate:
                case UnicodeCategory.OtherNotAssigned:
                    continue; // emoji / pictographs / zero-width joiners / variation selectors
                case UnicodeCategory.Control:
                    sb.Append(' ');
                    continue;
                default:
                    sb.Append(rune.ToString());
                    continue;
            }
        }

        // 4) Collapse the whitespace the removals left behind.
        return Whitespace().Replace(sb.ToString(), " ").Trim();
    }

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]*\)")]
    private static partial Regex MarkdownLink();

    // ** __ * _ ~~ ` runs, plus leading #-heading and >-quote markers.
    [GeneratedRegex(@"\*{1,3}|_{1,3}|~~|`+|(?:^|\n)[ \t]*#{1,6}[ \t]*|(?:^|\n)[ \t]*>[ \t]*")]
    private static partial Regex MarkdownMarkers();

    [GeneratedRegex(@"\s+")]
    private static partial Regex Whitespace();
}
