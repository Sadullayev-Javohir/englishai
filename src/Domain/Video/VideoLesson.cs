using Domain.Assessment;
using Domain.Common;

namespace Domain.Video;

/// <summary>
/// Aggregate root for a curated video lesson (PROJECT-SPEC B.3). The platform never
/// stores the video file itself - only the YouTube id (for embedding) plus the curated
/// interactive layer: a CEFR level, a timed transcript, and comprehension questions
/// (docs/development-guide.md rule 17.3). This curated corpus is the product's long-term moat (Qism A.2).
/// </summary>
public sealed class VideoLesson
{
    private readonly List<TranscriptSegment> _transcript = new();
    private readonly List<ComprehensionQuestion> _questions = new();
    private readonly List<VideoGlossaryEntry> _glossary = new();

    // Parameterless ctor for EF Core materialization.
    private VideoLesson()
    {
        YouTubeVideoId = null!;
        Title = null!;
        Channel = null!;
        Topic = null!;
    }

    private VideoLesson(
        string youTubeVideoId,
        string title,
        string channel,
        int durationSeconds,
        string topic,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        YouTubeVideoId = youTubeVideoId;
        Title = title;
        Channel = channel;
        DurationSeconds = durationSeconds;
        Topic = topic;
        CreatedAt = now;
        Status = IngestionStatus.Pending;
    }

    public Guid Id { get; private set; }

    /// <summary>The YouTube video id used to embed the player (no file is stored).</summary>
    public string YouTubeVideoId { get; private set; }

    public string Title { get; private set; }
    public string Channel { get; private set; }
    public int DurationSeconds { get; private set; }

    /// <summary>Topic/tag used for curation and recommendation (e.g. "technology").</summary>
    public string Topic { get; private set; }

    /// <summary>Auto-assigned CEFR level once <see cref="Status"/> is <see cref="IngestionStatus.Leveled"/>.</summary>
    public CefrLevel Level { get; private set; }

    public IngestionStatus Status { get; private set; }

    /// <summary>
    /// State of the interactive transcript fill (PROJECT-SPEC B.3, Bosqich 3). Starts
    /// <see cref="TranscriptStatus.Pending"/>; becomes <see cref="TranscriptStatus.Available"/>
    /// when real lines are set, or <see cref="TranscriptStatus.Unavailable"/> when a fill attempt
    /// (captions + STT) produced nothing - so the player can tell "still preparing" from
    /// "this video has no transcript" instead of spinning forever (docs/development-guide.md rule 8).
    /// </summary>
    public TranscriptStatus TranscriptStatus { get; private set; }

    public DateTimeOffset CreatedAt { get; private set; }

    public IReadOnlyList<TranscriptSegment> Transcript => _transcript;
    public IReadOnlyList<ComprehensionQuestion> Questions => _questions;

    /// <summary>
    /// Notable words from the transcript with short Uzbek meanings, used for the tap/hover
    /// "what does this word mean" affordance in the player. Filled together with the transcript
    /// translation (PROJECT-SPEC B.3, Bosqich 3); empty until then.
    /// </summary>
    public IReadOnlyList<VideoGlossaryEntry> Glossary => _glossary;

    /// <summary>
    /// Ingests a video from its YouTube metadata. The lesson starts <see cref="IngestionStatus.Pending"/>;
    /// a CEFR level is assigned afterwards by the leveler (PROJECT-SPEC B.3, Bosqich 1).
    /// </summary>
    public static VideoLesson Ingest(
        string youTubeVideoId,
        string title,
        string channel,
        int durationSeconds,
        string topic,
        IEnumerable<TranscriptSegment> transcript,
        DateTimeOffset now)
    {
        var lesson = CreateValidated(youTubeVideoId, title, channel, durationSeconds, topic, now);
        lesson.SetTranscriptInternal(transcript);
        return lesson;
    }

    /// <summary>
    /// Builds a fully curated lesson (level + transcript + questions known up front), as
    /// used by the seeded content catalog. The lesson is immediately <see cref="IngestionStatus.Leveled"/>.
    /// </summary>
    public static VideoLesson Curate(
        string youTubeVideoId,
        string title,
        string channel,
        int durationSeconds,
        string topic,
        CefrLevel level,
        IEnumerable<TranscriptSegment> transcript,
        IEnumerable<ComprehensionQuestion> questions,
        DateTimeOffset now)
    {
        var lesson = CreateValidated(youTubeVideoId, title, channel, durationSeconds, topic, now);
        lesson.SetTranscriptInternal(transcript);

        foreach (var question in questions)
        {
            if (question is null)
                throw new DomainException("Question must not be null.");
            lesson._questions.Add(question);
        }

        lesson.AssignLevel(level);
        return lesson;
    }

    /// <summary>
    /// Replaces the lesson's transcript with timed lines fetched from the video's real
    /// captions (PROJECT-SPEC B.3, Bosqich 3). Used to fill a lesson that was created with an
    /// empty transcript once its captions become available; never fabricated (docs/development-guide.md rule 8).
    /// </summary>
    public void SetTranscript(IEnumerable<TranscriptSegment> transcript)
    {
        _transcript.Clear();
        SetTranscriptInternal(transcript);
    }

    /// <summary>
    /// Appends a chunk of timed lines to the transcript as it streams in for a long video, without
    /// clearing what is already there (PROJECT-SPEC B.3, Bosqich 3 - progressive fill). While more
    /// chunks are coming the transcript is <see cref="TranscriptStatus.Partial"/> so the player
    /// shows the lines it has yet keeps polling; <paramref name="isFinal"/> on the last chunk flips
    /// it to <see cref="TranscriptStatus.Available"/>. Never fabricated (docs/development-guide.md rule 8).
    /// </summary>
    public void AppendTranscript(IEnumerable<TranscriptSegment> transcript, bool isFinal)
    {
        foreach (var segment in transcript.OrderBy(s => s.StartSeconds))
        {
            if (segment is null)
                throw new DomainException("Transcript segment must not be null.");
            _transcript.Add(segment);
        }

        if (_transcript.Count > 0)
            TranscriptStatus = isFinal ? TranscriptStatus.Available : TranscriptStatus.Partial;
    }

    /// <summary>
    /// Marks the transcript as terminally unavailable after a fill attempt (captions + STT)
    /// produced nothing, so the player shows an honest "no transcript" message and stops polling
    /// instead of spinning forever (docs/development-guide.md rule 8). No-op once a real transcript exists - a
    /// concurrent fill that succeeded must win.
    /// </summary>
    public void MarkTranscriptUnavailable()
    {
        if (_transcript.Count == 0)
            TranscriptStatus = TranscriptStatus.Unavailable;
    }

    /// <summary>
    /// Replaces the lesson's word glossary with translated entries (PROJECT-SPEC B.3, Bosqich 3).
    /// Filled when the transcript is translated; entries with empty word or meaning are skipped.
    /// </summary>
    public void SetGlossary(IEnumerable<VideoGlossaryEntry> entries)
    {
        _glossary.Clear();
        AddGlossary(entries);
    }

    /// <summary>
    /// Merges glossary entries into the existing list without clearing it, used by the chunked
    /// progressive fill so each transcript chunk contributes its words. Entries with an empty word
    /// or meaning, or whose word is already present (case-insensitive), are skipped.
    /// </summary>
    public void AddGlossary(IEnumerable<VideoGlossaryEntry> entries)
    {
        foreach (var entry in entries)
        {
            if (entry is null || string.IsNullOrWhiteSpace(entry.Word) || string.IsNullOrWhiteSpace(entry.UzbekMeaning))
                continue;
            var word = entry.Word.Trim();
            if (_glossary.Any(g => string.Equals(g.Word, word, StringComparison.OrdinalIgnoreCase)))
                continue;
            _glossary.Add(new VideoGlossaryEntry(word, entry.UzbekMeaning.Trim()));
        }
    }

    /// <summary>Assigns the CEFR level produced by the leveler and marks the lesson ready.</summary>
    public void AssignLevel(CefrLevel level)
    {
        Level = level;
        Status = IngestionStatus.Leveled;
    }

    /// <summary>
    /// Grades a learner's answers. Questions with no submitted answer count as incorrect,
    /// so the score reflects the whole quiz (PROJECT-SPEC Faza 4 comprehension check).
    /// </summary>
    public VideoQuizResult GradeQuiz(IReadOnlyDictionary<Guid, int> answers)
    {
        if (_questions.Count == 0)
            throw new DomainException("This lesson has no comprehension questions.");

        var outcomes = new List<QuestionOutcome>(_questions.Count);
        foreach (var question in _questions)
        {
            var hasAnswer = answers.TryGetValue(question.Id, out var selected);
            if (!hasAnswer)
                selected = -1;

            outcomes.Add(new QuestionOutcome(
                question.Id,
                selected,
                question.CorrectOptionIndex,
                hasAnswer && question.IsCorrect(selected),
                question.HintCode));
        }

        return new VideoQuizResult(Id, _questions.Count, outcomes.Count(o => o.IsCorrect), outcomes);
    }

    private static VideoLesson CreateValidated(
        string youTubeVideoId, string title, string channel, int durationSeconds, string topic, DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(youTubeVideoId))
            throw new DomainException("YouTube video id must not be empty.");
        if (string.IsNullOrWhiteSpace(title))
            throw new DomainException("Video title must not be empty.");
        if (string.IsNullOrWhiteSpace(channel))
            throw new DomainException("Video channel must not be empty.");
        if (durationSeconds <= 0)
            throw new DomainException("Video duration must be positive.");
        if (string.IsNullOrWhiteSpace(topic))
            throw new DomainException("Video topic must not be empty.");

        return new VideoLesson(
            youTubeVideoId.Trim(), title.Trim(), channel.Trim(), durationSeconds, topic.Trim(), now);
    }

    private void SetTranscriptInternal(IEnumerable<TranscriptSegment> transcript)
    {
        foreach (var segment in transcript.OrderBy(s => s.StartSeconds))
        {
            if (segment is null)
                throw new DomainException("Transcript segment must not be null.");
            _transcript.Add(segment);
        }

        if (_transcript.Count > 0)
            TranscriptStatus = TranscriptStatus.Available;
    }
}
