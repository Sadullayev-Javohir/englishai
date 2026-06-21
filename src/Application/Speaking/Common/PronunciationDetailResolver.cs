using Application.Speaking.Models;
using Application.Speaking.Ports;

namespace Application.Speaking.Common;

public static class PronunciationDetailResolver
{
    public static WordPhonetics? Resolve(IPhonemeVisualLibrary library, string word)
    {
        var originalWord = word.Trim();
        var spokenForm = EnglishSpokenForm.ToSpoken(originalWord);
        return library.GetWordPhonetics(spokenForm ?? originalWord);
    }

    public static bool IsSupported(IPhonemeVisualLibrary library, string word) =>
        Resolve(library, word) is not null;
}
