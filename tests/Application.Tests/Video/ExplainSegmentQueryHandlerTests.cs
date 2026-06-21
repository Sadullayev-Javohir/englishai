using Application.Common;
using Application.Video.Dtos;
using Application.Video.ExplainSegment;
using Application.Video.Ports;
using Domain.Assessment;
using Domain.Video;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Video;

/// <summary>
/// Verifies the explain-chat handler: it forwards to the explainer and trims/truncates history
/// server-side, and returns a null reply (honest "unavailable") rather than fabricating text when the
/// explainer cannot answer (rules 8, 10, 11).
/// </summary>
public class ExplainSegmentQueryHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 22, 10, 0, 0, TimeSpan.Zero);
    private readonly IChatExplainer _explainer = Substitute.For<IChatExplainer>();
    private readonly IVideoRepository _videos = Substitute.For<IVideoRepository>();
    private readonly IVideoExplainCache _cache = Substitute.For<IVideoExplainCache>();

    private ExplainSegmentQueryHandler Handler() => new(_explainer, _videos, _cache);

    private static VideoLesson Lesson() => VideoLesson.Curate(
        "abc123", "How habits change", "EnglishAI", 120, "habits", CefrLevel.B1,
        new[]
        {
            TranscriptSegment.Create(8, 12, "Small actions become routines.", null),
            TranscriptSegment.Create(0, 4, "Habits begin with a cue.", null),
        },
        Array.Empty<ComprehensionQuestion>(),
        Now);

    [Fact]
    public async Task Sends_the_complete_chronological_transcript_and_focus_to_the_explainer()
    {
        var lesson = Lesson();
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _explainer.ExplainAsync(
                "How habits change",
                "Habits begin with a cue.\nSmall actions become routines.",
                "Small actions become routines.",
                "Bu video nima haqida?",
                Arg.Any<IReadOnlyList<ChatTurn>>(),
                Arg.Any<CancellationToken>())
            .Returns("Bu video odatlar haqida.");

        var result = await Handler().Handle(
            new ExplainSegmentQuery(
                lesson.Id,
                " Small actions become routines. ",
                " Bu video nima haqida? ",
                Array.Empty<ChatTurnDto>()),
            CancellationToken.None);

        result.ReplyUz.Should().Be("Bu video odatlar haqida.");
    }

    [Fact]
    public async Task Returns_null_reply_when_explainer_is_unavailable()
    {
        var lesson = Lesson();
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _explainer.ExplainAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<IReadOnlyList<ChatTurn>>(), Arg.Any<CancellationToken>())
            .Returns((string?)null);

        var result = await Handler().Handle(
            new ExplainSegmentQuery(lesson.Id, "A sentence.", "Why this word?", Array.Empty<ChatTurnDto>()),
            CancellationToken.None);

        result.ReplyUz.Should().BeNull();
    }

    [Fact]
    public async Task Truncates_history_to_the_last_four_turns_before_calling_the_explainer()
    {
        var lesson = Lesson();
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var history = Enumerable.Range(1, 10)
            .Select(i => new ChatTurnDto(i % 2 == 0 ? "assistant" : "user", $"turn {i}"))
            .ToList();
        IReadOnlyList<ChatTurn>? captured = null;
        _explainer
            .ExplainAsync(
                Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                Arg.Do<IReadOnlyList<ChatTurn>>(h => captured = h), Arg.Any<CancellationToken>())
            .Returns("ok");

        await Handler().Handle(
            new ExplainSegmentQuery(lesson.Id, "A sentence.", "Follow-up?", history),
            CancellationToken.None);

        captured.Should().NotBeNull();
        captured!.Count.Should().Be(4);
        captured![0].Text.Should().Be("turn 7");
        captured![^1].Text.Should().Be("turn 10");
    }

    [Fact]
    public async Task Returns_cached_reply_without_calling_the_explainer()
    {
        var lesson = Lesson();
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _cache.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns("Cache javobi");

        var result = await Handler().Handle(
            new ExplainSegmentQuery(lesson.Id, "A sentence.", "Why?", Array.Empty<ChatTurnDto>()),
            CancellationToken.None);

        result.ReplyUz.Should().Be("Cache javobi");
        await _explainer.DidNotReceiveWithAnyArgs().ExplainAsync(default!, default!, default!, default!, default!, default);
    }

    [Fact]
    public async Task Throws_when_video_lesson_does_not_exist()
    {
        var missingId = Guid.NewGuid();
        _videos.GetByIdAsync(missingId, Arg.Any<CancellationToken>()).Returns((VideoLesson?)null);

        var act = () => Handler().Handle(
            new ExplainSegmentQuery(missingId, "", "Bu video nima haqida?", Array.Empty<ChatTurnDto>()),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }
}
