using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Speaking.PracticeWords;
using Domain.Speaking;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class GetSpeakingPracticeWordsQueryHandlerTests
{
    [Fact]
    public async Task Handle_returns_only_words_with_pronunciation_details()
    {
        var learnerId = Guid.NewGuid();
        var repository = Substitute.For<ISpeakingPracticeWordRepository>();
        var library = Substitute.For<IPhonemeVisualLibrary>();
        repository.GetActiveAsync(learnerId, Arg.Any<CancellationToken>()).Returns(new[]
        {
            SpeakingPracticeWord.Create(learnerId, "known", 55, PronunciationErrorType.Mispronunciation, DateTimeOffset.UtcNow),
            SpeakingPracticeWord.Create(learnerId, "unsupported", 45, PronunciationErrorType.Mispronunciation, DateTimeOffset.UtcNow),
        });
        library.GetWordPhonetics("known")
            .Returns(new WordPhonetics("known", "noʊn", Array.Empty<string>()));
        library.GetWordPhonetics("unsupported").Returns((WordPhonetics?)null);
        var handler = new GetSpeakingPracticeWordsQueryHandler(repository, library);

        var result = await handler.Handle(
            new GetSpeakingPracticeWordsQuery(learnerId), CancellationToken.None);

        result.Select(word => word.Word).Should().Equal("known");
    }

    [Fact]
    public async Task Handle_resolves_supported_spoken_forms()
    {
        var learnerId = Guid.NewGuid();
        var repository = Substitute.For<ISpeakingPracticeWordRepository>();
        var library = Substitute.For<IPhonemeVisualLibrary>();
        repository.GetActiveAsync(learnerId, Arg.Any<CancellationToken>()).Returns(new[]
        {
            SpeakingPracticeWord.Create(learnerId, "5:00", 55, PronunciationErrorType.Mispronunciation, DateTimeOffset.UtcNow),
        });
        library.GetWordPhonetics("five o'clock")
            .Returns(new WordPhonetics("five o'clock", "faɪv əklɑk", Array.Empty<string>()));
        var handler = new GetSpeakingPracticeWordsQueryHandler(repository, library);

        var result = await handler.Handle(
            new GetSpeakingPracticeWordsQuery(learnerId), CancellationToken.None);

        result.Should().ContainSingle().Which.Word.Should().Be("5:00");
    }
}
