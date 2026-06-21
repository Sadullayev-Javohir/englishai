using Application.Common;
using Application.Speaking.GetWordPronunciationDetail;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Domain.Speaking;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class GetWordPronunciationDetailQueryHandlerTests
{
    private readonly IPhonemeVisualLibrary _library = Substitute.For<IPhonemeVisualLibrary>();
    private readonly IFeedbackTemplateProvider _feedback = Substitute.For<IFeedbackTemplateProvider>();
    private readonly ITextToSpeechService _tts = Substitute.For<ITextToSpeechService>();
    private readonly IWordExampleProvider _wordExamples = Substitute.For<IWordExampleProvider>();
    private readonly FakeWordVisemeCache _cache = new();

    private GetWordPronunciationDetailQueryHandler CreateHandler() =>
        new(_library, _feedback, _tts, _cache, _wordExamples);

    private static SynthesizedSpeech Speech(string? animation, params VisemeFrame[] frames) =>
        new(new byte[] { 1, 2, 3 }, VisemeSequence.Create(frames, TimeSpan.FromMilliseconds(300), animation));

    [Fact]
    public async Task Handle_returns_ipa_visuals_uzbek_tip_and_the_synthesized_redlips_track()
    {
        _library.GetWordPhonetics("three")
            .Returns(new WordPhonetics("three", "θriː", new[] { "θ", "r", "iː" }));
        _library.GetByPhoneme("θ").Returns(new PhonemeVisual("θ", "viseme-th", 19, true, "tip.th"));
        _library.GetByPhoneme("r").Returns(new PhonemeVisual("r", "viseme-r", 13, true, null));
        _library.GetByPhoneme("iː").Returns((PhonemeVisual?)null);
        _feedback.Get("tip.th", null).Returns("Tilingizni old tishlaringiz orasiga qo'ying.");
        _tts.SynthesizeAsync("three", Arg.Any<CancellationToken>()).Returns(Speech(
            "<svg id=\"whole-word\"/>",
            new VisemeFrame(19, TimeSpan.Zero),
            new VisemeFrame(13, TimeSpan.FromMilliseconds(150))));

        var result = await CreateHandler().Handle(
            new GetWordPronunciationDetailQuery("three"), CancellationToken.None);

        result.Ipa.Should().Be("θriː");
        result.Phonemes.Should().HaveCount(3);
        result.Phonemes[0].SvgId.Should().Be("viseme-th");
        result.Phonemes[0].IsHardForUzbek.Should().BeTrue();
        result.Phonemes[2].SvgId.Should().Be("viseme-neutral"); // unknown phoneme falls back
        result.TipUz.Should().Be("Tilingizni old tishlaringiz orasiga qo'ying.");

        // The mouth animation is the single whole-word Azure red-lips SVG; the frames carry
        // viseme ids/offsets (ascending) for timing only.
        result.VisemeAnimation.Should().Be("<svg id=\"whole-word\"/>");
        result.Visemes.Should().HaveCount(2);
        result.Visemes[0].VisemeId.Should().Be(19);
        result.Visemes.Select(v => v.OffsetMs).Should().BeInAscendingOrder();

        // The reference audio is returned as base64 so the screen plays it directly
        // (the browser voice is an unreliable fallback on many Linux/Chromium clients).
        result.AudioBase64.Should().Be(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
    }

    [Fact]
    public async Task Handle_synthesizes_each_word_only_once_and_serves_the_cache_after()
    {
        _library.GetWordPhonetics("three")
            .Returns(new WordPhonetics("three", "θriː", new[] { "θ" }));
        _library.GetByPhoneme("θ").Returns(new PhonemeVisual("θ", "viseme-th", 19, true, null));
        _tts.SynthesizeAsync("three", Arg.Any<CancellationToken>())
            .Returns(Speech("<svg/>", new VisemeFrame(19, TimeSpan.Zero)));

        var handler = CreateHandler();
        await handler.Handle(new GetWordPronunciationDetailQuery("three"), CancellationToken.None);
        await handler.Handle(new GetWordPronunciationDetailQuery("three"), CancellationToken.None);

        await _tts.Received(1).SynthesizeAsync("three", Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_throws_when_word_is_unknown()
    {
        _library.GetWordPhonetics(Arg.Any<string>()).Returns((WordPhonetics?)null);

        var act = () => CreateHandler().Handle(
            new GetWordPronunciationDetailQuery("zzz"), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task Handle_resolves_a_numeric_time_through_its_spoken_form()
    {
        _library.GetWordPhonetics("five o'clock")
            .Returns(new WordPhonetics("five o'clock", "faɪv əklɑk", new[] { "f", "aɪ", "v" }));
        _tts.SynthesizeAsync("five o'clock", Arg.Any<CancellationToken>())
            .Returns(Speech("<svg/>", new VisemeFrame(0, TimeSpan.Zero)));

        var result = await CreateHandler().Handle(
            new GetWordPronunciationDetailQuery("5:00"), CancellationToken.None);

        result.Word.Should().Be("5:00");
        result.SpokenForm.Should().Be("five o'clock");
        result.Ipa.Should().Be("faɪv əklɑk");
        await _tts.Received(1).SynthesizeAsync("five o'clock", Arg.Any<CancellationToken>());
    }

    /// <summary>Minimal dictionary-backed <see cref="IWordVisemeCache"/> for the handler tests.</summary>
    private sealed class FakeWordVisemeCache : IWordVisemeCache
    {
        private readonly Dictionary<string, SynthesizedSpeech> _store = new(StringComparer.OrdinalIgnoreCase);

        public bool TryGet(string word, out SynthesizedSpeech speech) => _store.TryGetValue(word, out speech!);

        public void Set(string word, SynthesizedSpeech speech) => _store[word] = speech;
    }
}
