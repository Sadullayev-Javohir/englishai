using Application.Common;
using Application.Speaking.Dtos;
using Application.Speaking.Common;
using Application.Speaking.Ports;
using MediatR;

namespace Application.Speaking.GetWordPronunciationDetail;

public sealed class GetWordPronunciationDetailQueryHandler
    : IRequestHandler<GetWordPronunciationDetailQuery, WordPronunciationDetailDto>
{
    private const string NeutralSvgId = "viseme-neutral";

    private readonly IPhonemeVisualLibrary _library;
    private readonly IFeedbackTemplateProvider _feedback;
    private readonly ITextToSpeechService _tts;
    private readonly IWordVisemeCache _visemeCache;
    private readonly IWordExampleProvider _wordExamples;

    public GetWordPronunciationDetailQueryHandler(
        IPhonemeVisualLibrary library,
        IFeedbackTemplateProvider feedback,
        ITextToSpeechService tts,
        IWordVisemeCache visemeCache,
        IWordExampleProvider wordExamples)
    {
        _library = library;
        _feedback = feedback;
        _tts = tts;
        _visemeCache = visemeCache;
        _wordExamples = wordExamples;
    }

    public async Task<WordPronunciationDetailDto> Handle(
        GetWordPronunciationDetailQuery request,
        CancellationToken cancellationToken)
    {
        var originalWord = request.Word.Trim();
        var spokenForm = EnglishSpokenForm.ToSpoken(originalWord);
        var phonetics = PronunciationDetailResolver.Resolve(_library, originalWord)
            ?? throw new NotFoundException("WordPhonetics", request.Word);

        var visuals = phonetics.Phonemes
            .Select(p => _library.GetByPhoneme(p))
            .ToList();

        var phonemeDtos = phonetics.Phonemes
            .Zip(visuals, (phoneme, visual) => new PhonemeVisualDto(
                phoneme,
                visual?.SvgId ?? NeutralSvgId,
                visual?.IsHardForUzbek ?? false))
            .ToList();

        // Tip targets the first sound that is hard for Uzbek speakers and has a tip.
        var hardest = visuals.FirstOrDefault(v => v is { IsHardForUzbek: true, TipCode: not null });
        var tipUz = hardest?.TipCode is { } tipCode ? _feedback.Get(tipCode) : null;

        // The mouth animation is the official Azure red-lips SVG track for the whole word, and
        // the audio is the matching reference voice. Synthesize each word at most once and cache
        // both - every learner gets the same audio and mouth shapes (docs/development-guide.md rule 10). The
        // detail screen plays this audio because the browser's built-in speech synthesis is not
        // reliable (no installed voices on many Linux/Chromium clients leaves it silent).
        if (!_visemeCache.TryGet(phonetics.Word, out var speech))
        {
            speech = await _tts.SynthesizeAsync(phonetics.Word, cancellationToken);
            _visemeCache.Set(phonetics.Word, speech);
        }

        var visemes = speech.Visemes;
        var visemeDtos = VisemeFrameDto.FromDomain(visemes);
        var audioBase64 = speech.IsNaturalVoice && speech.AudioContent is { Length: > 0 }
            ? Convert.ToBase64String(speech.AudioContent)
            : null;

        // Keyword callout defaults to the word itself; the curated example sentence is
        // looked up from the vetted content store (rule 11 - null when not curated).
        var keyWord = phonetics.Word;
        var exampleSentenceUz = _wordExamples.GetExampleSentenceUz(phonetics.Word);

        var dto = new WordPronunciationDetailDto(
            originalWord, spokenForm, phonetics.Ipa, phonemeDtos, tipUz, keyWord, exampleSentenceUz,
            visemeDtos, visemes.Animation, audioBase64);
        return dto;
    }
}
