namespace Infrastructure.Speaking;

/// <summary>
/// Converts CMU ARPABET phoneme tokens (e.g. <c>L IH1 S AH0 N IH0 NG</c>) into the
/// IPA symbols used by <see cref="PhonemeVisualLibrary"/>. ARPABET vowels carry a
/// trailing stress digit (0/1/2); the digit is stripped for phoneme identity but the
/// primary stress (1) is used to place the IPA stress mark in the joined transcription.
/// </summary>
internal static class ArpabetIpaMap
{
    private const char PrimaryStress = '1';
    private const string StressMark = "ˈ";

    // ARPABET symbol (no stress digit) -> IPA. Schwa (AH0) is handled separately below.
    private static readonly IReadOnlyDictionary<string, string> Map = new Dictionary<string, string>
    {
        ["AA"] = "ɑː", ["AE"] = "æ", ["AH"] = "ʌ", ["AO"] = "ɔː", ["AW"] = "aʊ",
        ["AY"] = "aɪ", ["EH"] = "e", ["ER"] = "ɜː", ["EY"] = "eɪ", ["IH"] = "ɪ",
        ["IY"] = "iː", ["OW"] = "oʊ", ["OY"] = "ɔɪ", ["UH"] = "ʊ", ["UW"] = "uː",
        ["B"] = "b", ["CH"] = "tʃ", ["D"] = "d", ["DH"] = "ð", ["F"] = "f",
        ["G"] = "g", ["HH"] = "h", ["JH"] = "dʒ", ["K"] = "k", ["L"] = "l",
        ["M"] = "m", ["N"] = "n", ["NG"] = "ŋ", ["P"] = "p", ["R"] = "r",
        ["S"] = "s", ["SH"] = "ʃ", ["T"] = "t", ["TH"] = "θ", ["V"] = "v",
        ["W"] = "w", ["Y"] = "j", ["Z"] = "z", ["ZH"] = "ʒ",
    };

    /// <summary>
    /// Maps ARPABET tokens to ordered IPA phonemes plus a joined IPA transcription with
    /// a single primary-stress mark. Returns null if any token is unrecognized, so the
    /// caller can fall back rather than show a malformed word.
    /// </summary>
    public static (IReadOnlyList<string> Phonemes, string Ipa)? Convert(IReadOnlyList<string> arpabetTokens)
    {
        if (arpabetTokens.Count == 0)
            return null;

        var phonemes = new List<string>(arpabetTokens.Count);
        var ipa = new System.Text.StringBuilder();
        var stressPlaced = false;

        foreach (var token in arpabetTokens)
        {
            var hasPrimaryStress = token.Length > 0 && token[^1] == PrimaryStress;
            var symbol = StripStress(token);

            if (!Map.TryGetValue(symbol, out var phoneme))
                return null;

            // AH0 is the unstressed schwa /ə/, distinct from the stressed /ʌ/ (e.g. "cup").
            if (symbol == "AH" && !hasPrimaryStress && EndsWithUnstressed(token))
                phoneme = "ə";

            phonemes.Add(phoneme);

            if (hasPrimaryStress && !stressPlaced)
            {
                ipa.Append(StressMark);
                stressPlaced = true;
            }

            ipa.Append(phoneme);
        }

        return (phonemes, ipa.ToString());
    }

    private static string StripStress(string token) =>
        token.Length > 0 && char.IsDigit(token[^1]) ? token[..^1] : token;

    private static bool EndsWithUnstressed(string token) =>
        token.Length > 0 && token[^1] == '0';
}
