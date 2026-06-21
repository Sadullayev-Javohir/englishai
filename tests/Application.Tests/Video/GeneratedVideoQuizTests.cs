using Application.Common;
using Application.Gamification;
using Application.Gamification.Dtos;
using Application.Video.Dtos;
using Application.Video.GenerateVideoQuiz;
using Application.Video.Models;
using Application.Video.Ports;
using Application.Video.SubmitVideoQuiz;
using Domain.Assessment;
using Domain.Video;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Application.Tests.Video;

public sealed class GeneratedVideoQuizTests
{
    private readonly IVideoRepository _videos = Substitute.For<IVideoRepository>();
    private readonly IVideoQuizGenerator _generator = Substitute.For<IVideoQuizGenerator>();
    private readonly IVideoQuizStore _store = Substitute.For<IVideoQuizStore>();
    private readonly IDailyProgressRecorder _progress = Substitute.For<IDailyProgressRecorder>();
    private readonly Guid _learner = Guid.NewGuid();
    private static GeneratedVideoQuestion Question() => new(Guid.NewGuid(), "Who speaks?", "Kim gapiryapti?",
        ["Tim", "Tom", "Sam", "Joe"], 0, 1, 4, "I'm Tim.", "Men Timman.", "U o'zini Tim deb tanishtirdi.");
    private GeneratedVideoQuiz Session() => new(Guid.NewGuid(), Guid.NewGuid(), _learner, "Introduction",
        "I_tRSrPru94", DateTimeOffset.UtcNow.AddHours(1), [Question()]);
    private SubmitVideoQuizCommandHandler Grader() => new(_videos, Substitute.For<IVideoContentProvider>(), _progress, TimeProvider.System, _store);

    [Fact]
    public async Task Generates_from_real_transcript_and_withholds_answer_key()
    {
        var lesson = VideoLesson.Curate("I_tRSrPru94", "Introduction", "BBC", 150, "Introductions", CefrLevel.A2,
            [TranscriptSegment.Create(1, 4, "I'm Tim.")], [], DateTimeOffset.UtcNow);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _generator.GenerateAsync(lesson.Title, lesson.Level, lesson.Transcript, Arg.Any<CancellationToken>()).Returns(new[] { Question() });
        var dto = await new GenerateVideoQuizCommandHandler(_videos, _generator, _store, TimeProvider.System)
            .Handle(new(lesson.Id, _learner), CancellationToken.None);
        dto.Questions.Should().ContainSingle();
        dto.Result.Should().BeNull();
        typeof(GeneratedVideoQuestionDto).GetProperties().Should().NotContain(p => p.Name == "CorrectOptionIndex" || p.Name == "ExplanationUz");
        await _store.Received(1).SaveAsync(Arg.Is<GeneratedVideoQuiz>(q => q.LearnerId == _learner), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Empty_transcript_never_spends_an_AI_request()
    {
        var lesson = VideoLesson.Ingest("abc", "Title", "BBC", 90, "Topic", [], DateTimeOffset.UtcNow);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var act = () => new GenerateVideoQuizCommandHandler(_videos, _generator, _store, TimeProvider.System).Handle(new(lesson.Id, _learner), CancellationToken.None);
        (await act.Should().ThrowAsync<VideoQuizUnavailableException>()).Which.Code.Should().Be("transcript_unavailable");
        await _generator.DidNotReceiveWithAnyArgs().GenerateAsync(default!, default, default!, default);
    }

    [Fact]
    public async Task AI_failure_does_not_create_a_fake_quiz()
    {
        var lesson = VideoLesson.Ingest("abc", "Title", "BBC", 90, "Topic", [TranscriptSegment.Create(1, 4, "Hello.")], DateTimeOffset.UtcNow);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _generator.GenerateAsync(Arg.Any<string>(), Arg.Any<CefrLevel>(), Arg.Any<IReadOnlyList<TranscriptSegment>>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<GeneratedVideoQuestion>());
        var act = () => new GenerateVideoQuizCommandHandler(_videos, _generator, _store, TimeProvider.System).Handle(new(lesson.Id, _learner), CancellationToken.None);
        await act.Should().ThrowAsync<VideoQuizUnavailableException>();
        await _store.DidNotReceiveWithAnyArgs().SaveAsync(default!, default);
    }

    [Fact]
    public async Task Grades_a_complete_quiz_and_returns_authoritative_reward()
    {
        var quiz = Session();
        _store.GetAsync(quiz.Id, Arg.Any<CancellationToken>()).Returns(quiz);
        _progress.RecordSkillAsync(_learner, Domain.Learning.SkillType.Listening, 100, Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new SkillRewardDto(40, 0, 0, 0, 0, 0, false, 1, false));
        var result = await Grader().Handle(new(quiz.VideoLessonId, _learner, [new(quiz.Questions[0].Id, 0)], quiz.Id), CancellationToken.None);
        result.CorrectCount.Should().Be(1);
        result.AwardedXp.Should().Be(40);
        result.Outcomes[0].Hint.Should().Be(quiz.Questions[0].ExplanationUz);
        await _store.Received(1).SaveAsync(Arg.Is<GeneratedVideoQuiz>(s => s.Result == result), Arg.Any<CancellationToken>());
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(4)]
    public async Task Rejects_out_of_range_choices(int option)
    {
        var quiz = Session();
        _store.GetAsync(quiz.Id, Arg.Any<CancellationToken>()).Returns(quiz);
        var act = () => Grader().Handle(new(quiz.VideoLessonId, _learner, [new(quiz.Questions[0].Id, option)], quiz.Id), CancellationToken.None);
        await act.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Does_not_reveal_answers_for_partial_or_unknown_question_submissions()
    {
        var quiz = Session();
        _store.GetAsync(quiz.Id, Arg.Any<CancellationToken>()).Returns(quiz);
        var partial = () => Grader().Handle(new(quiz.VideoLessonId, _learner, [], quiz.Id), CancellationToken.None);
        await partial.Should().ThrowAsync<ValidationException>();
        var unknown = () => Grader().Handle(new(quiz.VideoLessonId, _learner, [new(Guid.NewGuid(), 0)], quiz.Id), CancellationToken.None);
        await unknown.Should().ThrowAsync<ValidationException>();
    }

    [Fact]
    public async Task Rejects_other_learners_and_expired_sessions()
    {
        var quiz = Session();
        _store.GetAsync(quiz.Id, Arg.Any<CancellationToken>()).Returns(quiz);
        var other = () => Grader().Handle(new(quiz.VideoLessonId, Guid.NewGuid(), [new(quiz.Questions[0].Id, 0)], quiz.Id), CancellationToken.None);
        await other.Should().ThrowAsync<ForbiddenException>();
        _store.GetAsync(quiz.Id, Arg.Any<CancellationToken>()).Returns(quiz with { ExpiresAt = DateTimeOffset.UtcNow.AddMinutes(-1) });
        var expired = () => new GetGeneratedVideoQuizQueryHandler(_store, TimeProvider.System).Handle(new(quiz.Id, _learner), CancellationToken.None);
        await expired.Should().ThrowAsync<VideoQuizUnavailableException>();
    }

    [Fact]
    public async Task Retry_returns_same_result_without_awarding_XP_again()
    {
        var quiz = Session();
        var result = new VideoQuizResultDto(quiz.VideoLessonId, 1, 1, 100, true, [], 40);
        _store.GetAsync(quiz.Id, Arg.Any<CancellationToken>()).Returns(quiz with { Result = result });
        var retried = await Grader().Handle(new(quiz.VideoLessonId, _learner, [new(quiz.Questions[0].Id, 1)], quiz.Id), CancellationToken.None);
        retried.Should().BeSameAs(result);
        await _progress.DidNotReceiveWithAnyArgs().RecordSkillAsync(default, default, default, default, default);
    }
}
