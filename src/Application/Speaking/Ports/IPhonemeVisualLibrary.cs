using Application.Speaking.Models;

namespace Application.Speaking.Ports;

/// <summary>
/// Content store mapping IPA phonemes to 2D SVG mouth shapes and English words to
/// their phonetic breakdown (PROJECT-SPEC B.2, 17.1). Lives in the content layer,
/// not in code.
/// </summary>
public interface IPhonemeVisualLibrary
{
    PhonemeVisual? GetByPhoneme(string ipaPhoneme);

    WordPhonetics? GetWordPhonetics(string word);
}
