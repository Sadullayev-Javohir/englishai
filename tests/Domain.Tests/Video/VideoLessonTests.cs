using Domain.Assessment;
using Domain.Common;
using Domain.Video;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Video;

public class VideoLessonTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    private static TranscriptSegment Seg(double start, double end, string text) =>
        TranscriptSegment.Create(start, end, text, "uzbek");

    private static ComprehensionQuestion Question(int correct = 0, string? hint = null) =>
        ComprehensionQuestion.Create("What is the main idea?", new[] { "A", "B", "C" }, correct, hint);

    private static VideoLesson Curated(params ComprehensionQuestion[] questions) =>
        VideoLesson.Curate(
            "abc123",
            "Success in AI",
            "BBC Learning English",
            400,
            "technology",
            CefrLevel.B1,
            new[] { Seg(2, 5, "Hello"), Seg(0, 2, "Intro") },
            questions.Length == 0 ? new[] { Question() } : questions,
            Now);

    [Fact]
    public void Ingest_starts_pending_and_orders_transcript_by_start_time()
    {
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news",
            new[] { Seg(5, 8, "second"), Seg(0, 4, "first") }, Now);

        lesson.Status.Should().Be(IngestionStatus.Pending);
        lesson.Transcript.Should().HaveCount(2);
        lesson.Transcript[0].EnglishText.Should().Be("first");
        lesson.Transcript[1].EnglishText.Should().Be("second");
    }

    [Fact]
    public void AssignLevel_marks_the_lesson_leveled()
    {
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news", new[] { Seg(0, 4, "first") }, Now);

        lesson.AssignLevel(CefrLevel.B2);

        lesson.Level.Should().Be(CefrLevel.B2);
        lesson.Status.Should().Be(IngestionStatus.Leveled);
    }

    [Fact]
    public void Ingest_with_a_transcript_marks_it_available()
    {
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news", new[] { Seg(0, 4, "first") }, Now);

        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Available);
    }

    [Fact]
    public void Ingest_with_no_transcript_stays_pending()
    {
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news", Array.Empty<TranscriptSegment>(), Now);

        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Pending);
    }

    [Fact]
    public void SetTranscript_with_real_lines_marks_it_available()
    {
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news", Array.Empty<TranscriptSegment>(), Now);

        lesson.SetTranscript(new[] { Seg(0, 2, "Hello") });

        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Available);
    }

    [Fact]
    public void AppendTranscript_keeps_it_partial_until_the_final_chunk_then_available()
    {
        // The progressive (chunked) fill of a long video: each non-final append leaves the lesson
        // Partial (player shows it, keeps polling); only the final append completes it.
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news", Array.Empty<TranscriptSegment>(), Now);

        lesson.AppendTranscript(new[] { Seg(0, 2, "Hello") }, isFinal: false);
        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Partial);

        lesson.AppendTranscript(new[] { Seg(2, 4, "Again") }, isFinal: true);
        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Available);
        lesson.Transcript.Should().HaveCount(2);
        lesson.Transcript[0].EnglishText.Should().Be("Hello");
        lesson.Transcript[1].EnglishText.Should().Be("Again");
    }

    [Fact]
    public void AddGlossary_merges_without_clearing_and_skips_duplicate_words()
    {
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news", Array.Empty<TranscriptSegment>(), Now);

        lesson.AddGlossary(new[] { new VideoGlossaryEntry("welcome", "xush kelibsiz") });
        // A later chunk repeats "welcome" (case-insensitively) and adds a new word.
        lesson.AddGlossary(new[]
        {
            new VideoGlossaryEntry("Welcome", "qabul"),
            new VideoGlossaryEntry("lesson", "dars"),
        });

        lesson.Glossary.Should().HaveCount(2);
        lesson.Glossary[0].Word.Should().Be("welcome");
        lesson.Glossary[0].UzbekMeaning.Should().Be("xush kelibsiz");
        lesson.Glossary[1].Word.Should().Be("lesson");
    }

    [Fact]
    public void MarkTranscriptUnavailable_sets_the_terminal_state_when_empty()
    {
        var lesson = VideoLesson.Ingest(
            "abc123", "Title", "Channel", 300, "news", Array.Empty<TranscriptSegment>(), Now);

        lesson.MarkTranscriptUnavailable();

        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Unavailable);
    }

    [Fact]
    public void MarkTranscriptUnavailable_is_ignored_once_a_transcript_exists()
    {
        // A concurrent fill that succeeded must win over a late "give up" call.
        var lesson = Curated();

        lesson.MarkTranscriptUnavailable();

        lesson.TranscriptStatus.Should().Be(TranscriptStatus.Available);
    }

    [Fact]
    public void Curate_produces_a_leveled_lesson_ready_to_show()
    {
        var lesson = Curated();

        lesson.Status.Should().Be(IngestionStatus.Leveled);
        lesson.Level.Should().Be(CefrLevel.B1);
        lesson.Questions.Should().HaveCount(1);
    }

    [Theory]
    [InlineData("", "Title")]
    [InlineData("abc123", "")]
    public void Ingest_rejects_missing_required_metadata(string videoId, string title)
    {
        var act = () => VideoLesson.Ingest(
            videoId, title, "Channel", 300, "news", new[] { Seg(0, 4, "x") }, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Ingest_rejects_non_positive_duration()
    {
        var act = () => VideoLesson.Ingest(
            "abc123", "Title", "Channel", 0, "news", new[] { Seg(0, 4, "x") }, Now);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void GradeQuiz_scores_correct_answers_and_passes_above_threshold()
    {
        var q1 = Question(correct: 1);
        var q2 = Question(correct: 2);
        var q3 = Question(correct: 0);
        var lesson = Curated(q1, q2, q3);

        var result = lesson.GradeQuiz(new Dictionary<Guid, int>
        {
            [q1.Id] = 1, // correct
            [q2.Id] = 2, // correct
            [q3.Id] = 1, // wrong
        });

        result.TotalQuestions.Should().Be(3);
        result.CorrectCount.Should().Be(2);
        result.ScorePercent.Should().Be(67);
        result.Passed.Should().BeFalse(); // 67% < 70%
    }

    [Fact]
    public void GradeQuiz_passes_at_full_marks_and_reveals_correct_answers()
    {
        var q1 = Question(correct: 1, hint: "hint.consistency");
        var q2 = Question(correct: 2);
        var lesson = Curated(q1, q2);

        var result = lesson.GradeQuiz(new Dictionary<Guid, int> { [q1.Id] = 1, [q2.Id] = 2 });

        result.Passed.Should().BeTrue();
        result.ScorePercent.Should().Be(100);
        result.Outcomes.Should().HaveCount(2);
        result.Outcomes[0].CorrectOptionIndex.Should().Be(1);
        result.Outcomes[0].HintCode.Should().Be("hint.consistency");
    }

    [Fact]
    public void GradeQuiz_counts_unanswered_questions_as_incorrect()
    {
        var q1 = Question(correct: 1);
        var q2 = Question(correct: 2);
        var lesson = Curated(q1, q2);

        var result = lesson.GradeQuiz(new Dictionary<Guid, int> { [q1.Id] = 1 });

        result.CorrectCount.Should().Be(1);
        result.Outcomes.Single(o => o.QuestionId == q2.Id).IsCorrect.Should().BeFalse();
        result.Outcomes.Single(o => o.QuestionId == q2.Id).SelectedOptionIndex.Should().Be(-1);
    }
}

public class ComprehensionQuestionTests
{
    [Fact]
    public void Create_rejects_out_of_range_correct_index()
    {
        var act = () => ComprehensionQuestion.Create("Q?", new[] { "A", "B" }, 5);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_rejects_too_few_options()
    {
        var act = () => ComprehensionQuestion.Create("Q?", new[] { "A" }, 0);

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void IsCorrect_matches_the_configured_answer()
    {
        var question = ComprehensionQuestion.Create("Q?", new[] { "A", "B", "C" }, 2);

        question.IsCorrect(2).Should().BeTrue();
        question.IsCorrect(0).Should().BeFalse();
    }
}

public class TranscriptSegmentTests
{
    [Fact]
    public void Create_rejects_end_before_start()
    {
        var act = () => TranscriptSegment.Create(5, 3, "text");

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void Create_trims_text_and_keeps_optional_translation()
    {
        var segment = TranscriptSegment.Create(0, 3, "  hello ", "  salom ");

        segment.EnglishText.Should().Be("hello");
        segment.UzbekTranslation.Should().Be("salom");
    }

    [Fact]
    public void Create_keeps_supplied_words_ordered_by_start()
    {
        var segment = TranscriptSegment.Create(0, 3, "hello there friend", words: new[]
        {
            TranscriptWord.Create("there", 1.0, 1.5),
            TranscriptWord.Create("hello", 0.0, 0.9),
            TranscriptWord.Create("friend", 2.0, 2.6),
        });

        segment.Words.Select(w => w.Text).Should().Equal("hello", "there", "friend");
    }

    [Fact]
    public void Create_without_words_leaves_them_empty_for_the_estimation_fallback()
    {
        TranscriptSegment.Create(0, 3, "hello there").Words.Should().BeEmpty();
    }
}

/// <summary>Covers the per-word value object used by the player's karaoke highlight.</summary>
public class TranscriptWordTests
{
    [Theory]
    [InlineData("", 0, 1)]
    [InlineData("  ", 0, 1)]
    public void Create_rejects_empty_text(string text, double start, double end) =>
        new Action(() => TranscriptWord.Create(text, start, end)).Should().Throw<DomainException>();

    [Theory]
    [InlineData(-0.1, 1)]   // negative start
    [InlineData(1, 1)]      // end == start
    [InlineData(2, 1)]      // end < start
    public void Create_rejects_an_invalid_span(double start, double end) =>
        new Action(() => TranscriptWord.Create("word", start, end)).Should().Throw<DomainException>();
}
