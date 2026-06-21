using Application.Speaking.Models;
using Application.Speaking.Ports;

namespace Infrastructure.Speaking;

/// <summary>
/// Content store mapping IPA phonemes to 2D SVG mouth shapes (PROJECT-SPEC B.2) and
/// a starter dictionary of word phonetics. Sounds that are typically hard for Uzbek
/// speakers (th, English r, w vs v, æ, short/long i) are flagged and carry an Uzbek
/// tip code resolved by the feedback template provider.
/// </summary>
public sealed class PhonemeVisualLibrary : IPhonemeVisualLibrary
{
    // SVG mouth-shape id → Azure viseme id (0..21). Phonemes that share an SVG shape
    // share a viseme id; ids match the articulation buckets the frontend VisemeMouth
    // renders (VisemeMouth.tsx). Anything missing falls back to 0 (neutral/silence).
    // Declared before Phonemes because BuildPhonemes() reads it during static init.
    private static readonly IReadOnlyDictionary<string, int> SvgIdToVisemeId =
        new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["viseme-neutral"] = 0,
            ["viseme-ae"] = 1, ["viseme-uh"] = 1, ["viseme-ah"] = 1,
            ["viseme-aa"] = 2,
            ["viseme-ao"] = 3,
            ["viseme-eh"] = 4,
            ["viseme-er"] = 5,
            ["viseme-iy"] = 6, ["viseme-ih"] = 6, ["viseme-y"] = 6,
            ["viseme-uw"] = 7, ["viseme-w"] = 7,
            ["viseme-ow"] = 8,
            ["viseme-aw"] = 9,
            ["viseme-oy"] = 10,
            ["viseme-ay"] = 11, ["viseme-ey"] = 11,
            ["viseme-h"] = 12,
            ["viseme-r"] = 13,
            ["viseme-l"] = 14,
            ["viseme-s"] = 15, ["viseme-z"] = 15,
            ["viseme-sh"] = 16, ["viseme-ch"] = 16, ["viseme-jh"] = 16, ["viseme-zh"] = 16,
            ["viseme-f"] = 18, ["viseme-v"] = 18,
            ["viseme-th"] = 19, ["viseme-d"] = 19, ["viseme-t"] = 19, ["viseme-n"] = 19,
            ["viseme-k"] = 20, ["viseme-g"] = 20, ["viseme-ng"] = 20,
            ["viseme-p"] = 21, ["viseme-b"] = 21, ["viseme-m"] = 21,
        };

    private static readonly IReadOnlyDictionary<string, PhonemeVisual> Phonemes = BuildPhonemes();
    private static readonly IReadOnlyDictionary<string, WordPhonetics> Words = BuildWords();

    public PhonemeVisual? GetByPhoneme(string ipaPhoneme) =>
        Phonemes.GetValueOrDefault(ipaPhoneme);

    public WordPhonetics? GetWordPhonetics(string word)
    {
        var normalized = word.Trim().ToLowerInvariant();
        if (normalized.Length == 0)
            return null;

        // Single curated/dictionary word: the common case.
        if (ResolveSingleWord(normalized) is { } single)
            return single;

        // Multi-word phrases ("bus stop") and hyphenated compounds ("well-known") are
        // taught as one vocabulary entry but the CMU dictionary only holds single words,
        // so resolving the whole string returns null. Split into parts, resolve each, and
        // stitch them back into one phonetic breakdown: the IPA parts are space-joined and
        // the phonemes concatenated, while the Word keeps the original phrase so the TTS
        // synthesizes (and the viseme cache keys) the whole phrase. If any part is unknown
        // the phrase is treated as unresolved (returns null → caller's NotFound).
        var parts = normalized.Split(PhraseSeparators, StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2)
            return null;

        var ipaParts = new List<string>(parts.Length);
        var phonemes = new List<string>();
        foreach (var part in parts)
        {
            if (ResolveSingleWord(part) is not { } resolved)
                return null;
            ipaParts.Add(resolved.Ipa);
            phonemes.AddRange(resolved.Phonemes);
        }

        return new WordPhonetics(normalized, string.Join(" ", ipaParts), phonemes);
    }

    // Resolves one whitespace-free word: curated words keep hand-tuned IPA; everything
    // else falls back to the CMU dictionary so any real English word resolves instead of
    // returning 404. Returns null when the word is unknown.
    private static WordPhonetics? ResolveSingleWord(string word)
    {
        if (Words.TryGetValue(word, out var curated))
            return curated;

        var arpabet = CmuPronunciationDictionary.Instance.GetArpabet(word);
        if (arpabet is null)
            return null;

        var converted = ArpabetIpaMap.Convert(arpabet);
        if (converted is not { } result)
            return null;

        return new WordPhonetics(word, result.Ipa, result.Phonemes);
    }

    private static readonly char[] PhraseSeparators = { ' ', '\t', '-' };

    private static Dictionary<string, PhonemeVisual> BuildPhonemes()
    {
        PhonemeVisual P(string p, string svg, bool hard = false, string? tip = null) =>
            new(p, svg, SvgIdToVisemeId.GetValueOrDefault(svg, 0), hard, tip);

        var list = new[]
        {
            // Hard-for-Uzbek sounds (flagged with tips)
            P("θ", "viseme-th", true, "tip.th"),
            P("ð", "viseme-th", true, "tip.th"),
            P("r", "viseme-r", true, "tip.r"),
            P("ɹ", "viseme-r", true, "tip.r"),
            P("w", "viseme-w", true, "tip.w"),
            P("v", "viseme-v", true, "tip.v"),
            P("æ", "viseme-ae", true, "tip.ae"),
            P("ɪ", "viseme-ih", true, "tip.ih"),
            P("iː", "viseme-iy", true, "tip.ih"),
            // Further sounds Uzbek speakers routinely miss. Uzbek has no reduced vowel, so "ə" is
            // pronounced full-strength; "ŋ" is read as "n"+"g"; and the voiced/voiceless pairs
            // (s/z, ch/j) collapse. Each now carries its own tip instead of falling through silently.
            P("ə", "viseme-uh", true, "tip.schwa"),
            P("ʌ", "viseme-ah", true, "tip.uh"),
            P("ŋ", "viseme-ng", true, "tip.ng"),
            P("z", "viseme-z", true, "tip.z"),
            P("ʃ", "viseme-sh", true, "tip.sh"),
            P("tʃ", "viseme-ch", true, "tip.ch"),
            P("dʒ", "viseme-jh", true, "tip.dzh"),
            P("uː", "viseme-uw", true, "tip.oo"),
            P("ʊ", "viseme-uh", true, "tip.oo"),
            P("h", "viseme-h", true, "tip.h"),
            P("l", "viseme-l", true, "tip.l"),
            // Remaining common phonemes
            P("i", "viseme-iy"), P("e", "viseme-eh"), P("ɜː", "viseme-er"),
            P("ɑː", "viseme-aa"),
            P("ɔː", "viseme-ao"), P("oʊ", "viseme-ow"), P("aɪ", "viseme-ay"), P("eɪ", "viseme-ey"),
            P("aʊ", "viseme-aw"), P("ɔɪ", "viseme-oy"), P("ʒ", "viseme-zh"),
            P("f", "viseme-f"), P("s", "viseme-s"),
            P("j", "viseme-y"),
            P("d", "viseme-d"), P("t", "viseme-t"), P("p", "viseme-p"),
            P("b", "viseme-b"), P("m", "viseme-m"), P("n", "viseme-n"),
            P("k", "viseme-k"), P("g", "viseme-g")
        };

        return list.ToDictionary(v => v.Phoneme);
    }

    private static Dictionary<string, WordPhonetics> BuildWords()
    {
        WordPhonetics W(string word, string ipa, params string[] phonemes) => new(word, ipa, phonemes);

        var list = new[]
        {
            W("three", "θriː", "θ", "r", "iː"),
            W("the", "ðə", "ð", "ə"),
            W("this", "ðɪs", "ð", "ɪ", "s"),
            W("think", "θɪŋk", "θ", "ɪ", "ŋ", "k"),
            W("water", "ˈwɔːtər", "w", "ɔː", "t", "ə", "r"),
            W("world", "wɜːld", "w", "ɜː", "l", "d"),
            W("very", "ˈveri", "v", "e", "r", "i"),
            W("cat", "kæt", "k", "æ", "t"),
            W("ship", "ʃɪp", "ʃ", "ɪ", "p"),
            W("sheep", "ʃiːp", "ʃ", "iː", "p"),
            W("right", "raɪt", "r", "aɪ", "t"),
            W("light", "laɪt", "l", "aɪ", "t"),
            W("hello", "həˈloʊ", "h", "ə", "l", "oʊ")
        };

        return list.ToDictionary(w => w.Word);
    }
}
