using Domain.Assessment;
using Domain.Video;

namespace Application.Video.Dtos;

/// <summary>A catalog row for the Video Lesson Preview screen (no transcript/quiz body).</summary>
public sealed record VideoSummaryDto(
    Guid Id,
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    string Topic,
    CefrLevel Level)
{
    public static VideoSummaryDto FromDomain(VideoLesson lesson) =>
        new(lesson.Id, lesson.YouTubeVideoId, lesson.Title, lesson.Channel,
            lesson.DurationSeconds, lesson.Topic, lesson.Level);
}

/// <summary>
/// One card in the infinite video feed (PROJECT-SPEC B.3, Bosqich 2). <see cref="LessonId"/>
/// is set when the video is already a stored lesson (full transcript/quiz flow available);
/// otherwise it is <c>null</c> and the client opens it on demand by <see cref="YouTubeVideoId"/>.
/// </summary>
public sealed record VideoFeedItemDto(
    Guid? LessonId,
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    string Topic,
    CefrLevel Level,
    bool HasClosedCaptions,
    string? ChannelAvatarUrl = null);

/// <summary>
/// A page of the video feed: items plus an opaque <see cref="NextCursor"/> for the next page
/// (<c>null</c> when there are no more). The cursor is round-tripped to <c>GetVideoFeedQuery</c>.
/// </summary>
public sealed record VideoFeedDto(
    IReadOnlyList<VideoFeedItemDto> Items,
    string? NextCursor);

/// <summary>One word of a transcript line with its own timing, for the player's karaoke highlight.</summary>
public sealed record TranscriptWordDto(string Text, double StartSeconds, double EndSeconds)
{
    public static TranscriptWordDto FromDomain(TranscriptWord word) =>
        new(word.Text, word.StartSeconds, word.EndSeconds);
}

/// <summary>
/// The state of a real-time (non-persisted) transcript fetch for a learner-pasted video, mirroring the
/// orchestrator's outcome. The client maps it to a vetted Uzbek message (docs/development-guide.md rule 11).
/// </summary>
public enum RealtimeTranscriptStatus
{
    /// <summary>Lines were fetched live and are present.</summary>
    Available,

    /// <summary>The video genuinely has no caption track - terminal; show "no transcript".</summary>
    Unavailable,

    /// <summary>Every source failed transiently - the client may retry shortly.</summary>
    Pending,
}

/// <summary>
/// A real-time transcript fetched live for a learner-pasted video and returned WITHOUT being stored
/// (docs/development-guide.md real-time rule). <see cref="Lines"/> is non-empty only when <see cref="Status"/> is
/// <see cref="RealtimeTranscriptStatus.Available"/>.
/// </summary>
public sealed record RealtimeTranscriptDto(
    RealtimeTranscriptStatus Status,
    IReadOnlyList<TranscriptSegmentDto> Lines);

/// <summary>
/// A timed transcript line (one sentence) for the interactive transcript (Video Player screen).
/// <see cref="Words"/> carries the line's per-word timing so the client can highlight the exact
/// spoken word; it is empty when the source gave only line-level timing (client then estimates).
/// </summary>
public sealed record TranscriptSegmentDto(
    double StartSeconds,
    double EndSeconds,
    string EnglishText,
    string? UzbekTranslation,
    IReadOnlyList<TranscriptWordDto> Words)
{
    public static TranscriptSegmentDto FromDomain(TranscriptSegment segment) =>
        new(segment.StartSeconds, segment.EndSeconds, segment.EnglishText, segment.UzbekTranslation,
            // Order defensively: owned collections come back from the DB without a guaranteed order,
            // and the player's karaoke highlight relies on words being chronological.
            segment.Words.OrderBy(w => w.StartSeconds).Select(TranscriptWordDto.FromDomain).ToList());
}

/// <summary>
/// A quiz question as shown to the learner - the correct answer is intentionally omitted
/// so grading happens server-side (revealed only in the <see cref="VideoQuizResultDto"/>).
/// </summary>
public sealed record QuizQuestionDto(
    Guid Id,
    string Prompt,
    IReadOnlyList<string> Options,
    string? HintCode)
{
    public static QuizQuestionDto FromDomain(ComprehensionQuestion question) =>
        new(question.Id, question.Prompt, question.Options, question.HintCode);
}

/// <summary>One transcript word with a short Uzbek meaning, for the player's tap/hover lookup.</summary>
public sealed record GlossaryEntryDto(string Word, string UzbekMeaning)
{
    public static GlossaryEntryDto FromDomain(VideoGlossaryEntry entry) =>
        new(entry.Word, entry.UzbekMeaning);
}

/// <summary>Full lesson detail: embed id, transcript, word glossary and (answerless) quiz questions.</summary>
public sealed record VideoLessonDto(
    Guid Id,
    string YouTubeVideoId,
    string Title,
    string Channel,
    int DurationSeconds,
    string Topic,
    CefrLevel Level,
    IngestionStatus Status,
    TranscriptStatus TranscriptStatus,
    IReadOnlyList<TranscriptSegmentDto> Transcript,
    IReadOnlyList<GlossaryEntryDto> Glossary,
    IReadOnlyList<QuizQuestionDto> Questions)
{
    public static VideoLessonDto FromDomain(VideoLesson lesson) =>
        new(lesson.Id, lesson.YouTubeVideoId, lesson.Title, lesson.Channel, lesson.DurationSeconds,
            lesson.Topic, lesson.Level, lesson.Status, lesson.TranscriptStatus,
            // Order by start: owned transcript segments are not read back in a guaranteed order, and
            // both the rendered transcript and the synced highlight need them chronological.
            lesson.Transcript.OrderBy(s => s.StartSeconds).Select(TranscriptSegmentDto.FromDomain).ToList(),
            lesson.Glossary.Select(GlossaryEntryDto.FromDomain).ToList(),
            lesson.Questions.Select(QuizQuestionDto.FromDomain).ToList());
}

/// <summary>
/// The graded outcome of one question, with the correct answer revealed. <see cref="Hint"/>
/// holds the vetted Uzbek hint (resolved from the question's hint code, docs/development-guide.md rule 11)
/// and is only populated for an incorrect answer.
/// </summary>
public sealed record QuestionOutcomeDto(
    Guid QuestionId,
    int SelectedOptionIndex,
    int CorrectOptionIndex,
    bool IsCorrect,
    string? Hint);

/// <summary>The result of submitting a comprehension quiz (PROJECT-SPEC Faza 4).</summary>
public sealed record VideoQuizResultDto(
    Guid VideoLessonId,
    int TotalQuestions,
    int CorrectCount,
    int ScorePercent,
    bool Passed,
    IReadOnlyList<QuestionOutcomeDto> Outcomes,
    int AwardedXp = 0);

/// <summary>One turn of the explain-chat panel, as the client sends/receives it.</summary>
public sealed record ChatTurnDto(string Role, string Text);

/// <summary>
/// The explain-chat panel's reply to one learner question. <see cref="ReplyUz"/> is null when the
/// explainer is unavailable/failed, so the UI shows an honest "javob olinmadi" state instead of
/// fabricated text (rules 8, 11).
/// </summary>
public sealed record ChatReplyDto(string? ReplyUz);
