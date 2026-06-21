using System.Text.RegularExpressions;

namespace Application.Speaking.Common;

/// <summary>
/// Lightweight intent detection over a learner's recognized utterance. Used to spot when the learner
/// is trying to tell the tutor their name - spoken self-introductions (especially spelled-out names
/// and Uzbek names) are exactly what the English speech-to-text model mis-transcribes, so when we see
/// the intent but have no stored name we prompt the learner to type it instead of letting a garbled
/// transcript stand. Pure text matching; no external calls.
/// </summary>
public static partial class SpeakingIntent
{
    /// <summary>
    /// True when the utterance looks like the learner introducing or spelling their name
    /// (e.g. "my name is ...", "call me ...", "I spell it ..."). Deliberately narrow so ordinary
    /// sentences ("I am happy") do not trigger it.
    /// </summary>
    public static bool LooksLikeNameIntroduction(string? recognizedText)
    {
        if (string.IsNullOrWhiteSpace(recognizedText))
            return false;

        return NameIntroductionPattern().IsMatch(recognizedText);
    }

    // "my name is", "my name's", "the name is", "i am/'m called", "(they) call me",
    // and "i spell ..." / "spell it/as" / "it's spelled" (spelling a name out letter by letter).
    [GeneratedRegex(
        @"\b(my name is|my name's|the name is|i('m| am) called|call me|i spell|spell (it|that|as)|it'?s spelled|spelled (it|as))\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex NameIntroductionPattern();
}
