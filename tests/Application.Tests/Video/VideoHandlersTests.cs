using Application.Common;
using Application.Gamification;
using Application.Learning.Ports;
using Application.Tests.Learning;
using Application.Video.Dtos;
using Application.Video.FillVideoTranscript;
using Application.Video.GetVideoCatalog;
using Application.Video.GetVideoFeed;
using Application.Video.GetVideoLesson;
using Application.Video.IngestVideo;
using Application.Video.Models;
using Application.Video.OpenVideo;
using Application.Video.OpenVideoByUrl;
using Application.Video.Ports;
using Application.Video.RateVideoDifficulty;
using Application.Video.SearchVideo;
using Application.Video.SubmitVideoQuiz;
using Domain.Assessment;
using Domain.Learning;
using Domain.Video;
using System.Collections.Generic;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Video;

public class VideoHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IVideoRepository _videos = Substitute.For<IVideoRepository>();
    private readonly IVideoTranscriptProvider _transcripts = Substitute.For<IVideoTranscriptProvider>();
    private readonly IVideoTranscriptTranslator _translator = Substitute.For<IVideoTranscriptTranslator>();
    private readonly IVideoTranscriptFiller _filler = Substitute.For<IVideoTranscriptFiller>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);
    private readonly IDailyProgressRecorder _dailyProgress = Substitute.For<IDailyProgressRecorder>();

    public VideoHandlersTests()
    {
        // Default: the provider ran and the video has no caption track, so lesson queries return
        // the lesson unchanged and the fill settles on the terminal "unavailable" state.
        _transcripts.FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(TranscriptFetchResult.NoCaptions);
        // Default: no LLM translation (honest "pending" English-only fill).
        _translator.TranslateAsync(Arg.Any<IReadOnlyList<TranscriptLine>>(), Arg.Any<CancellationToken>())
            .Returns(TranscriptTranslation.Empty);
    }

    private static ComprehensionQuestion Question(int correct) =>
        ComprehensionQuestion.Create("What is the key message?", new[] { "A", "B", "C" }, correct, "hint.x");

    private static LearnerProfile ProfileAt(CefrLevel level)
    {
        var placement = new PlacementResult(level, level.ToScore(),
            new Dictionary<TestStage, StageResult>());
        return LearnerProfile.CreateFromPlacement(Learner, placement, Now);
    }

    private static VideoLesson Lesson(params ComprehensionQuestion[] questions) =>
        VideoLesson.Curate(
            "abc123", "Success in AI", "BBC Learning English", 400, "technology", CefrLevel.B1,
            new[] { TranscriptSegment.Create(0, 3, "Hello", "Salom") },
            questions.Length == 0 ? new[] { Question(0) } : questions,
            Now);

    [Fact]
    public async Task SearchVideo_does_not_pad_query_with_unrelated_curated_results()
    {
        var feed = Substitute.For<IVideoFeedSource>();
        feed.SearchAsync(Arg.Any<string>(), Arg.Is<string?>(value => value == null), 12, Arg.Any<CancellationToken>())
            .Returns(new VideoFeedPage(
                new[] { new VideoFeedResult("song1", "A Thousand Years lyrics", "Music Channel", 270, ChannelAvatarUrl: "https://yt3.ggpht.com/music-channel") },
                null));
        _videos.GetByYouTubeIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, VideoLesson>());
        var handler = new SearchVideoQueryHandler(feed, _videos);

        var page = await handler.Handle(new SearchVideoQuery("thousand years", null, 12), CancellationToken.None);

        page.Items.Should().ContainSingle();
        page.Items[0].Title.Should().Be("A Thousand Years lyrics");
        page.Items[0].ChannelAvatarUrl.Should().Be("https://yt3.ggpht.com/music-channel");
        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task IngestVideo_fetches_metadata_levels_it_and_persists()
    {
        var metadata = Substitute.For<IYouTubeMetadataProvider>();
        metadata.FetchAsync("abc123", Arg.Any<CancellationToken>()).Returns(new VideoMetadata(
            "abc123", "Success in AI", "BBC Learning English", 400,
            new[] { new TranscriptLine(0, 3, "Hello world") }));
        var leveler = Substitute.For<ICefrVideoLeveler>();
        leveler.EstimateLevelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(CefrLevel.B2);

        var handler = new IngestVideoCommandHandler(metadata, leveler, _videos, _clock);

        var dto = await handler.Handle(new IngestVideoCommand("abc123", "technology"), CancellationToken.None);

        dto.Level.Should().Be(CefrLevel.B2);
        dto.Status.Should().Be(IngestionStatus.Leveled);
        dto.Transcript.Should().ContainSingle();
        await _videos.Received(1).SaveAsync(
            Arg.Is<VideoLesson>(v => v.Level == CefrLevel.B2 && v.Status == IngestionStatus.Leveled),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVideoCatalog_uses_the_learners_level()
    {
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        _videos.GetCatalogForLevelAsync(CefrLevel.B1, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new[] { Lesson() });
        var handler = new GetVideoCatalogQueryHandler(_videos, profiles);

        var catalog = await handler.Handle(new GetVideoCatalogQuery(Learner), CancellationToken.None);

        catalog.Should().ContainSingle();
        await _videos.Received(1).GetCatalogForLevelAsync(CefrLevel.B1, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVideoCatalog_defaults_to_A2_when_no_profile()
    {
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        _videos.GetCatalogForLevelAsync(CefrLevel.A2, Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VideoLesson>());
        var handler = new GetVideoCatalogQueryHandler(_videos, profiles);

        await handler.Handle(new GetVideoCatalogQuery(Learner), CancellationToken.None);

        await _videos.Received(1).GetCatalogForLevelAsync(CefrLevel.A2, Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVideoLesson_throws_when_missing()
    {
        _videos.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VideoLesson?)null);
        var handler = new GetVideoLessonQueryHandler(_videos, _filler);

        var act = () => handler.Handle(new GetVideoLessonQuery(Guid.NewGuid()), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetVideoLesson_withholds_correct_answers()
    {
        var lesson = Lesson(Question(2));
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new GetVideoLessonQueryHandler(_videos, _filler);

        var dto = await handler.Handle(new GetVideoLessonQuery(lesson.Id), CancellationToken.None);

        dto.Questions.Should().ContainSingle();
        dto.Questions[0].Options.Should().HaveCount(3);
        // The DTO has no CorrectOptionIndex property - grading is server-side only.
        dto.Questions[0].GetType().GetProperty("CorrectOptionIndex").Should().BeNull();
    }

    [Fact]
    public async Task GetVideoLesson_requests_a_background_fill_when_transcript_empty()
    {
        // Opening a not-yet-transcribed lesson must return immediately (no blocking fetch) and
        // kick off the transcript fill off the request path so the player loads fast.
        var lesson = StoredLesson("vidT", CefrLevel.A2); // created with an empty transcript
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new GetVideoLessonQueryHandler(_videos, _filler);

        var dto = await handler.Handle(new GetVideoLessonQuery(lesson.Id), CancellationToken.None);

        dto.Transcript.Should().BeEmpty();
        _filler.Received(1).RequestFill(lesson.Id);
    }

    [Fact]
    public async Task GetVideoLesson_does_not_request_a_fill_when_transcript_present()
    {
        var lesson = Lesson(); // curated with a transcript already
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new GetVideoLessonQueryHandler(_videos, _filler);

        await handler.Handle(new GetVideoLessonQuery(lesson.Id), CancellationToken.None);

        _filler.DidNotReceive().RequestFill(Arg.Any<Guid>());
    }

    [Fact]
    public async Task GetVideoLesson_does_not_retry_a_terminally_unavailable_transcript()
    {
        var lesson = StoredLesson("vidUnavailable", CefrLevel.A2);
        lesson.MarkTranscriptUnavailable();
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new GetVideoLessonQueryHandler(_videos, _filler);

        var dto = await handler.Handle(new GetVideoLessonQuery(lesson.Id), CancellationToken.None);

        dto.TranscriptStatus.Should().Be(TranscriptStatus.Unavailable);
        _filler.DidNotReceive().RequestFill(Arg.Any<Guid>());
    }

    [Fact]
    public async Task FillVideoTranscript_fills_and_persists_transcript_from_captions()
    {
        var lesson = StoredLesson("vidT", CefrLevel.A2); // created with an empty transcript
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _transcripts.FetchAsync("vidT", Arg.Any<CancellationToken>()).Returns(TranscriptFetchResult.Fetched(new[]
        {
            new TranscriptLine(0, 2, "Hello there."),
            new TranscriptLine(2, 4, "Welcome to the lesson."),
        }));
        var handler = new FillVideoTranscriptCommandHandler(_videos, _transcripts, _translator);

        var filled = await handler.Handle(new FillVideoTranscriptCommand(lesson.Id), CancellationToken.None);

        filled.Should().BeTrue();
        lesson.Transcript.Should().HaveCount(2);
        lesson.Transcript[0].EnglishText.Should().Be("Hello there.");
        await _videos.Received(1).SaveAsync(lesson, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FillVideoTranscript_translates_lines_and_fills_glossary_when_translator_available()
    {
        var lesson = StoredLesson("vidTr", CefrLevel.A2);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        _transcripts.FetchAsync("vidTr", Arg.Any<CancellationToken>()).Returns(TranscriptFetchResult.Fetched(new[]
        {
            new TranscriptLine(0, 2, "Hello there."),
            new TranscriptLine(2, 4, "Welcome to the lesson."),
        }));
        _translator.TranslateAsync(Arg.Any<IReadOnlyList<TranscriptLine>>(), Arg.Any<CancellationToken>())
            .Returns(new TranscriptTranslation(
                new string?[] { "Salom.", "Darsga xush kelibsiz." },
                new[] { new WordGloss("welcome", "xush kelibsiz") }));
        var handler = new FillVideoTranscriptCommandHandler(_videos, _transcripts, _translator);

        await handler.Handle(new FillVideoTranscriptCommand(lesson.Id), CancellationToken.None);

        lesson.Transcript[0].UzbekTranslation.Should().Be("Salom.");
        lesson.Transcript[1].UzbekTranslation.Should().Be("Darsga xush kelibsiz.");
        lesson.Glossary.Should().ContainSingle();
        lesson.Glossary[0].Word.Should().Be("welcome");
        lesson.Glossary[0].UzbekMeaning.Should().Be("xush kelibsiz");
    }

    [Fact]
    public async Task FillVideoTranscript_streams_a_long_transcript_in_chunks_first_partial_then_available()
    {
        // A long video (well over the single-shot threshold): the fill must surface the opening lines
        // fast as a Partial transcript and stitch the rest on, only completing on the final chunk
        // (PROJECT-SPEC B.3 - progressive fill so the player shows text without waiting for it all).
        var lesson = StoredLesson("vidLong", CefrLevel.B1);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);

        const int lineCount = 300;
        var lines = Enumerable.Range(0, lineCount)
            .Select(i => new TranscriptLine(i * 2, i * 2 + 1.5, $"Line {i}."))
            .ToArray();
        _transcripts.FetchAsync("vidLong", Arg.Any<CancellationToken>()).Returns(TranscriptFetchResult.Fetched(lines));

        // Capture the transcript state at each save so we can assert the first save was Partial.
        var statusAtSave = new List<(int Count, TranscriptStatus Status)>();
        _videos.SaveAsync(lesson, Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask)
            .AndDoes(_ => statusAtSave.Add((lesson.Transcript.Count, lesson.TranscriptStatus)));

        var handler = new FillVideoTranscriptCommandHandler(_videos, _transcripts, _translator);

        var filled = await handler.Handle(new FillVideoTranscriptCommand(lesson.Id), CancellationToken.None);

        filled.Should().BeTrue();
        statusAtSave.Should().HaveCountGreaterThan(1, "a long transcript is streamed in several chunks");
        // First chunk is ~1% (300 * 0.01 = 3), but the eight-line floor ensures useful opening
        // dialogue lands immediately while the remaining chunks continue in the background.
        statusAtSave[0].Count.Should().Be(8);
        statusAtSave[0].Status.Should().Be(TranscriptStatus.Partial);
        // Only the final save completes the transcript with every line present, in order.
        statusAtSave[^1].Status.Should().Be(TranscriptStatus.Available);
        lesson.Transcript.Should().HaveCount(lineCount);
        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Available);
        lesson.Transcript[0].EnglishText.Should().Be("Line 0.");
        lesson.Transcript[^1].EnglishText.Should().Be($"Line {lineCount - 1}.");
    }

    [Fact]
    public async Task FillVideoTranscript_shows_the_full_english_transcript_before_translating()
    {
        // The player renders English only, so display must never wait on the LLM: the whole transcript
        // is appended (Available) first, and translation runs afterwards purely to enrich the lesson
        // (docs/development-guide.md rule 8 - never block display on the slow path).
        var lesson = StoredLesson("vidFast", CefrLevel.B1);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);

        const int lineCount = 300;
        var lines = Enumerable.Range(0, lineCount)
            .Select(i => new TranscriptLine(i * 2, i * 2 + 1.5, $"Line {i}."))
            .ToArray();
        _transcripts.FetchAsync("vidFast", Arg.Any<CancellationToken>()).Returns(TranscriptFetchResult.Fetched(lines));

        // Snapshot the transcript the moment the translator is first invoked.
        int englishCountAtFirstTranslate = -1;
        TranscriptStatus statusAtFirstTranslate = TranscriptStatus.Pending;
        _translator.TranslateAsync(Arg.Any<IReadOnlyList<TranscriptLine>>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                if (englishCountAtFirstTranslate < 0)
                {
                    englishCountAtFirstTranslate = lesson.Transcript.Count;
                    statusAtFirstTranslate = lesson.TranscriptStatus;
                }
                return TranscriptTranslation.Empty;
            });

        var handler = new FillVideoTranscriptCommandHandler(_videos, _transcripts, _translator);

        await handler.Handle(new FillVideoTranscriptCommand(lesson.Id), CancellationToken.None);

        // By the first translation the entire transcript is already shown and complete.
        englishCountAtFirstTranslate.Should().Be(lineCount);
        statusAtFirstTranslate.Should().Be(TranscriptStatus.Available);
    }

    [Fact]
    public async Task FillVideoTranscript_marks_unavailable_when_captions_yield_nothing()
    {
        // No caption track: the transcript settles on the terminal "unavailable" state (persisted) so
        // the player stops polling and shows an honest message (rule 8) - it does NOT stay forever
        // "pending", and there is no speech-to-text fallback.
        var empty = StoredLesson("vidNone", CefrLevel.A2);
        _videos.GetByIdAsync(empty.Id, Arg.Any<CancellationToken>()).Returns(empty);
        var handler = new FillVideoTranscriptCommandHandler(_videos, _transcripts, _translator); // default: no captions

        var filled = await handler.Handle(new FillVideoTranscriptCommand(empty.Id), CancellationToken.None);

        filled.Should().BeFalse();
        empty.Transcript.Should().BeEmpty();
        empty.TranscriptStatus.Should().Be(TranscriptStatus.Unavailable);
        await _videos.Received(1).SaveAsync(empty, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FillVideoTranscript_leaves_lesson_pending_when_provider_is_unavailable()
    {
        // A transient provider failure (yt-dlp missing, rate-limit, network) must NOT settle the
        // lesson on the terminal "unavailable" state - otherwise a single hiccup permanently
        // poisons an otherwise-captioned video into "no transcript". It stays Pending to retry
        // on the next open, and nothing is persisted (docs/development-guide.md rule 8).
        var pending = StoredLesson("vidFlaky", CefrLevel.A2);
        _videos.GetByIdAsync(pending.Id, Arg.Any<CancellationToken>()).Returns(pending);
        _transcripts.FetchAsync("vidFlaky", Arg.Any<CancellationToken>())
            .Returns(TranscriptFetchResult.ProviderUnavailable);
        var handler = new FillVideoTranscriptCommandHandler(_videos, _transcripts, _translator);

        var filled = await handler.Handle(new FillVideoTranscriptCommand(pending.Id), CancellationToken.None);

        filled.Should().BeFalse();
        pending.Transcript.Should().BeEmpty();
        pending.TranscriptStatus.Should().Be(TranscriptStatus.Pending);
        await _videos.DidNotReceive().SaveAsync(pending, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task FillVideoTranscript_skips_fetch_when_transcript_already_present()
    {
        // A lesson already filled by a concurrent request must not trigger another fetch.
        var withTranscript = Lesson();
        _videos.GetByIdAsync(withTranscript.Id, Arg.Any<CancellationToken>()).Returns(withTranscript);
        var handler = new FillVideoTranscriptCommandHandler(_videos, _transcripts, _translator);

        var filled = await handler.Handle(new FillVideoTranscriptCommand(withTranscript.Id), CancellationToken.None);

        filled.Should().BeFalse();
        await _transcripts.DidNotReceive().FetchAsync(withTranscript.YouTubeVideoId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitVideoQuiz_grades_against_the_lesson()
    {
        var q1 = Question(1);
        var q2 = Question(2);
        var lesson = Lesson(q1, q2);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var content = Substitute.For<IVideoContentProvider>();
        content.GetHint("hint.x").Returns("E'tibor bering: ma'ruzachi asosiy fikrni takrorladi.");
        var handler = new SubmitVideoQuizCommandHandler(_videos, content, _dailyProgress, _clock);

        var result = await handler.Handle(
            new SubmitVideoQuizCommand(lesson.Id, Learner, new[]
            {
                new QuizAnswer(q1.Id, 1), // correct
                new QuizAnswer(q2.Id, 0), // wrong
            }),
            CancellationToken.None);

        result.TotalQuestions.Should().Be(2);
        result.CorrectCount.Should().Be(1);
        var correct = result.Outcomes.Single(o => o.QuestionId == q1.Id);
        correct.IsCorrect.Should().BeTrue();
        correct.CorrectOptionIndex.Should().Be(1);
        correct.Hint.Should().BeNull(); // no hint for a correct answer
        result.Outcomes.Single(o => o.QuestionId == q2.Id).Hint
            .Should().Be("E'tibor bering: ma'ruzachi asosiy fikrni takrorladi.");
        await _dailyProgress.DidNotReceive().RecordSkillAsync(
            Learner, SkillType.Listening, Arg.Any<int>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitVideoQuiz_passing_attempt_awards_listening_progress()
    {
        var q1 = Question(1);
        var q2 = Question(2);
        var lesson = Lesson(q1, q2);
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var handler = new SubmitVideoQuizCommandHandler(
            _videos, Substitute.For<IVideoContentProvider>(), _dailyProgress, _clock);

        var result = await handler.Handle(
            new SubmitVideoQuizCommand(lesson.Id, Learner, new[]
            {
                new QuizAnswer(q1.Id, 1),
                new QuizAnswer(q2.Id, 2),
            }), CancellationToken.None);

        result.Passed.Should().BeTrue();
        await _dailyProgress.Received(1).RecordSkillAsync(
            Learner, SkillType.Listening, Arg.Any<int>(), Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RateVideoDifficulty_records_the_signal()
    {
        var lesson = Lesson();
        _videos.GetByIdAsync(lesson.Id, Arg.Any<CancellationToken>()).Returns(lesson);
        var feedback = Substitute.For<IVideoFeedbackStore>();
        var handler = new RateVideoDifficultyCommandHandler(_videos, feedback, _clock);

        await handler.Handle(
            new RateVideoDifficultyCommand(Learner, lesson.Id, VideoDifficultyRating.TooHard),
            CancellationToken.None);

        await feedback.Received(1).RecordRatingAsync(
            Learner, lesson.Id, VideoDifficultyRating.TooHard, Now, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task RateVideoDifficulty_throws_when_lesson_missing()
    {
        _videos.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VideoLesson?)null);
        var handler = new RateVideoDifficultyCommandHandler(
            _videos, Substitute.For<IVideoFeedbackStore>(), _clock);

        var act = () => handler.Handle(
            new RateVideoDifficultyCommand(Learner, Guid.NewGuid(), VideoDifficultyRating.JustRight),
            CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    private static VideoLesson StoredLesson(string youTubeVideoId, CefrLevel level) =>
        VideoLesson.Curate(
            youTubeVideoId, "Stored lesson", "Stored channel", 240, "listening", level,
            Array.Empty<TranscriptSegment>(), Array.Empty<ComprehensionQuestion>(), Now);

    [Fact]
    public async Task GetVideoFeed_first_page_maps_results_and_tags_stored_lessons()
    {
        var feed = Substitute.For<IVideoFeedSource>();
        feed.SearchAsync(Arg.Any<string>(), Arg.Is<string?>(c => c == null), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new VideoFeedPage(
                new[]
                {
                    new VideoFeedResult("vid1", "Title 1", "Channel 1", 120, HasClosedCaptions: true, ChannelAvatarUrl: "https://yt3.ggpht.com/channel-1"),
                    new VideoFeedResult("vid2", "Title 2", "Channel 2", 200, HasClosedCaptions: true),
                },
                "cont-1"));

        var stored = StoredLesson("vid1", CefrLevel.A2);
        _videos.GetByYouTubeIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, VideoLesson> { ["vid1"] = stored });

        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        var handler = new GetVideoFeedQueryHandler(feed, _videos, profiles);

        var page = await handler.Handle(new GetVideoFeedQuery(Learner, null, 12), CancellationToken.None);

        page.Items.Should().HaveCount(2);
        page.Items[0].LessonId.Should().Be(stored.Id);
        page.Items[0].Level.Should().Be(CefrLevel.A2); // stored lesson's real level
        page.Items[0].HasClosedCaptions.Should().BeTrue();
        page.Items[0].ChannelAvatarUrl.Should().Be("https://yt3.ggpht.com/channel-1");
        page.Items[1].LessonId.Should().BeNull();
        page.Items[1].Level.Should().Be(CefrLevel.B1); // learner level for not-yet-stored videos
        page.Items[1].HasClosedCaptions.Should().BeTrue();
        page.NextCursor.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public async Task GetVideoFeed_defaults_to_A2_search_when_no_profile()
    {
        var feed = Substitute.For<IVideoFeedSource>();
        feed.SearchAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new VideoFeedPage(Array.Empty<VideoFeedResult>(), null));
        _videos.GetByYouTubeIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, VideoLesson>());
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns((LearnerProfile?)null);
        var handler = new GetVideoFeedQueryHandler(feed, _videos, profiles);

        await handler.Handle(new GetVideoFeedQuery(Learner, null, 12), CancellationToken.None);

        await feed.Received(1).SearchAsync(
            Arg.Is<string>(s => s.Contains("a2")), Arg.Is<string?>(c => c == null), Arg.Any<int>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task GetVideoFeed_visit_seed_varies_the_first_page_search()
    {
        var feed = Substitute.For<IVideoFeedSource>();
        feed.SearchAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new VideoFeedPage(Array.Empty<VideoFeedResult>(), null));
        _videos.GetByYouTubeIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, VideoLesson>());
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.A2));
        var handler = new GetVideoFeedQueryHandler(feed, _videos, profiles);

        await handler.Handle(new GetVideoFeedQuery(Learner, null, 12, "visit-a"), CancellationToken.None);
        await handler.Handle(new GetVideoFeedQuery(Learner, null, 12, "visit-b"), CancellationToken.None);

        var calls = feed.ReceivedCalls()
            .Where(call => call.GetMethodInfo().Name == nameof(IVideoFeedSource.SearchAsync))
            .Select(call => (string)call.GetArguments()[0]!)
            .ToArray();
        calls.Should().HaveCount(2);
        calls[0].Should().NotBe(calls[1]);
    }

    [Fact]
    public async Task GetVideoFeed_no_continuation_yields_null_next_cursor()
    {
        var feed = Substitute.For<IVideoFeedSource>();
        feed.SearchAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new VideoFeedPage(new[] { new VideoFeedResult("vid1", "T", "C", 90) }, null));
        _videos.GetByYouTubeIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, VideoLesson>());
        var profiles = Substitute.For<ILearnerProfileRepository>();
        profiles.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.A1));
        var handler = new GetVideoFeedQueryHandler(feed, _videos, profiles);

        var page = await handler.Handle(new GetVideoFeedQuery(Learner, null, 12), CancellationToken.None);

        page.NextCursor.Should().BeNull();
    }

    [Fact]
    public async Task GetVideoFeed_next_page_reuses_search_term_and_continuation_without_profile_lookup()
    {
        var feed = Substitute.For<IVideoFeedSource>();
        feed.SearchAsync(Arg.Any<string>(), Arg.Any<string?>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new VideoFeedPage(Array.Empty<VideoFeedResult>(), "cont-2"));
        _videos.GetByYouTubeIdsAsync(Arg.Any<IReadOnlyCollection<string>>(), Arg.Any<CancellationToken>())
            .Returns(new Dictionary<string, VideoLesson>());

        var profiles1 = Substitute.For<ILearnerProfileRepository>();
        profiles1.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(ProfileAt(CefrLevel.B1));
        var firstPage = await new GetVideoFeedQueryHandler(feed, _videos, profiles1)
            .Handle(new GetVideoFeedQuery(Learner, null, 12), CancellationToken.None);

        var profiles2 = Substitute.For<ILearnerProfileRepository>();
        await new GetVideoFeedQueryHandler(feed, _videos, profiles2)
            .Handle(new GetVideoFeedQuery(Learner, firstPage.NextCursor, 12), CancellationToken.None);

        // The continuation page round-trips the B1 search term + source continuation, and
        // does not re-read the learner profile.
        await feed.Received(1).SearchAsync(
            Arg.Is<string>(s => s.Contains("b1")), "cont-2", Arg.Any<int>(), Arg.Any<CancellationToken>());
        await profiles2.DidNotReceive().GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenVideo_returns_existing_lesson_without_saving()
    {
        var stored = StoredLesson("vid1", CefrLevel.B1);
        _videos.GetByYouTubeIdAsync("vid1", Arg.Any<CancellationToken>()).Returns(stored);
        var handler = new OpenVideoCommandHandler(_videos, _clock);

        var dto = await handler.Handle(
            new OpenVideoCommand("vid1", "Ignored", "Ignored", 100, "listening", CefrLevel.A1),
            CancellationToken.None);

        dto.Id.Should().Be(stored.Id);
        await _videos.DidNotReceive().SaveAsync(Arg.Any<VideoLesson>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenVideo_creates_a_playable_lesson_with_no_fabricated_content()
    {
        _videos.GetByYouTubeIdAsync("vid9", Arg.Any<CancellationToken>()).Returns((VideoLesson?)null);
        var handler = new OpenVideoCommandHandler(_videos, _clock);

        var dto = await handler.Handle(
            new OpenVideoCommand("vid9", "Real Title", "Real Channel", 333, "listening", CefrLevel.B2),
            CancellationToken.None);

        dto.YouTubeVideoId.Should().Be("vid9");
        dto.Level.Should().Be(CefrLevel.B2);
        dto.Status.Should().Be(IngestionStatus.Leveled);
        dto.Transcript.Should().BeEmpty();
        dto.Questions.Should().BeEmpty();
        await _videos.Received(1).SaveAsync(
            Arg.Is<VideoLesson>(v => v.YouTubeVideoId == "vid9" && v.Level == CefrLevel.B2),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenVideoByUrl_returns_existing_lesson_without_fetching_or_saving()
    {
        var stored = StoredLesson("paste1", CefrLevel.C1);
        _videos.GetByYouTubeIdAsync("paste1", Arg.Any<CancellationToken>()).Returns(stored);
        var metadata = Substitute.For<IYouTubeMetadataProvider>();
        var leveler = Substitute.For<ICefrVideoLeveler>();
        var handler = new OpenVideoByUrlCommandHandler(_videos, metadata, leveler, _clock);

        var dto = await handler.Handle(new OpenVideoByUrlCommand("paste1"), CancellationToken.None);

        dto.Id.Should().Be(stored.Id);
        await metadata.DidNotReceive().FetchAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        await _videos.DidNotReceive().SaveAsync(Arg.Any<VideoLesson>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenVideoByUrl_creates_a_playable_lesson_from_real_metadata()
    {
        _videos.GetByYouTubeIdAsync("paste2", Arg.Any<CancellationToken>()).Returns((VideoLesson?)null);
        var metadata = Substitute.For<IYouTubeMetadataProvider>();
        metadata.FetchAsync("paste2", Arg.Any<CancellationToken>()).Returns(new VideoMetadata(
            "paste2", "Pasted Title", "Pasted Channel", 250,
            new[] { new TranscriptLine(0, 3, "Hello there") }));
        var leveler = Substitute.For<ICefrVideoLeveler>();
        leveler.EstimateLevelAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(CefrLevel.B1);
        var handler = new OpenVideoByUrlCommandHandler(_videos, metadata, leveler, _clock);

        var dto = await handler.Handle(new OpenVideoByUrlCommand("paste2"), CancellationToken.None);

        dto.YouTubeVideoId.Should().Be("paste2");
        dto.Title.Should().Be("Pasted Title");
        dto.Level.Should().Be(CefrLevel.B1);
        dto.Status.Should().Be(IngestionStatus.Leveled);
        // The interactive transcript is filled lazily from real captions on open - never fabricated here.
        dto.Transcript.Should().BeEmpty();
        await _videos.Received(1).SaveAsync(
            Arg.Is<VideoLesson>(v => v.YouTubeVideoId == "paste2" && v.Level == CefrLevel.B1),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task OpenVideoByUrl_falls_back_to_honest_metadata_when_fetch_fails()
    {
        _videos.GetByYouTubeIdAsync("paste3", Arg.Any<CancellationToken>()).Returns((VideoLesson?)null);
        var metadata = Substitute.For<IYouTubeMetadataProvider>();
        metadata.FetchAsync("paste3", Arg.Any<CancellationToken>())
            .Returns<VideoMetadata>(_ => throw new InvalidOperationException("no key"));
        var leveler = Substitute.For<ICefrVideoLeveler>();
        var handler = new OpenVideoByUrlCommandHandler(_videos, metadata, leveler, _clock);

        var dto = await handler.Handle(new OpenVideoByUrlCommand("paste3"), CancellationToken.None);

        dto.YouTubeVideoId.Should().Be("paste3");
        dto.Status.Should().Be(IngestionStatus.Leveled);
        await _videos.Received(1).SaveAsync(
            Arg.Is<VideoLesson>(v => v.YouTubeVideoId == "paste3"),
            Arg.Any<CancellationToken>());
    }
}
