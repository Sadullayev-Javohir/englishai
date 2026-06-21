using Domain.Assessment;
using Domain.Common;

namespace Domain.Speaking;

/// <summary>
/// Aggregate root for an AI speaking-practice conversation. Holds the ordered turn
/// history and the learner's CEFR level, which the tutor uses to pitch its replies.
/// </summary>
public sealed class ConversationSession
{
    /// <summary>
    /// A pause longer than this between two learner utterances is not fully counted toward the
    /// engaged-speaking timer - so leaving the tab open does not inflate <see cref="SpokenTime"/>.
    /// </summary>
    public static readonly TimeSpan MaxCountedGap = TimeSpan.FromMinutes(2);

    /// <summary>
    /// The engaged-speaking cap for a single conversation. Once <see cref="SpokenTime"/> reaches this
    /// the sitting is over: each turn is a paid pipeline (STT + LLM + TTS), so one conversation is
    /// bounded to keep cost predictable (docs/development-guide.md rule 10). The learner can start a fresh
    /// conversation afterward, subject to the separate daily Speaking limit. Set generously (a long
    /// practice conversation, not a short demo) so an engaged learner is not cut off mid-flow - the
    /// gap between turns (thinking, listening to the tutor) counts toward this, so a real dialogue
    /// accumulates it faster than pure talk-time; the per-day limit is the primary cost guard.
    /// </summary>
    public static readonly TimeSpan MaxSpokenTime = TimeSpan.FromMinutes(30);

    /// <summary>
    /// How many of a conversation's opening learner turns are scored for pronunciation. Azure bills
    /// pronunciation assessment per audio hour on the SAME audio speech-to-text already charged for,
    /// so scoring every turn pays twice for one utterance - the conversation is a fluency exercise,
    /// not a pronunciation drill (docs/development-guide.md rule 10: assessment belongs to short dedicated
    /// exercises). The opening turns are always scored so the learner gets feedback immediately.
    /// </summary>
    public const int InitialAssessedTurns = 2;

    /// <summary>
    /// After the opening turns, every n-th learner turn is scored, so pronunciation feedback keeps
    /// appearing through a long conversation instead of stopping after the start. With the default
    /// values a twelve-turn sitting scores turns 1, 2, 4, 8 and 12 - five assessments instead of
    /// twelve, for the same teaching signal.
    /// </summary>
    public const int AssessedTurnInterval = 4;

    private readonly List<ConversationTurn> _turns = new();
    private readonly List<string> _focusWords = new();
    private DateTimeOffset? _lastActivityAt;

    private ConversationSession(
        Guid id,
        Guid learnerId,
        CefrLevel level,
        string? topic,
        IEnumerable<string> focusWords,
        Guid? vocabularyTopicId,
        string? learnerName,
        string? scenarioCode,
        SpeakingCurriculumContext? curriculumContext)
    {
        Id = id;
        LearnerId = learnerId;
        Level = level;
        Topic = topic;
        VocabularyTopicId = vocabularyTopicId;
        LearnerName = learnerName;
        ScenarioCode = scenarioCode;
        CurriculumContext = curriculumContext;
        _focusWords.AddRange(focusWords);
    }

    public Guid Id { get; }
    public Guid LearnerId { get; }
    public CefrLevel Level { get; }

    /// <summary>
    /// The name the learner asked the tutor to call them by (their stored preferred name), or null
    /// when they have not provided one. The tutor uses it to address the learner and is instructed
    /// never to invent a name when this is null - the only name source the tutor ever has.
    /// </summary>
    public string? LearnerName { get; private set; }

    /// <summary>
    /// The vocabulary topic this conversation practices, when the learner launched it from a
    /// studied topic. Null for a free conversation. Used to credit engaged speaking time toward
    /// learning that topic (5 minutes of speaking marks it learned).
    /// </summary>
    public Guid? VocabularyTopicId { get; }

    /// <summary>
    /// When set, this conversation is a roleplay: the tutor plays the persona of the
    /// <see cref="RoleplayScenarioCatalog"/> scenario with this code (interviewer, waiter, doctor, …)
    /// instead of being a generic conversation partner, and the sitting can be scored at the end
    /// (<see cref="RoleplayEvaluation"/>). Null for an ordinary free/topic conversation.
    /// </summary>
    public string? ScenarioCode { get; }

    public SpeakingCurriculumContext? CurriculumContext { get; private set; }

    /// <summary>True when this session is a roleplay (a persona scenario was chosen).</summary>
    public bool IsRoleplay => ScenarioCode is not null;

    /// <summary>The number of turns the learner has actually spoken in this session.</summary>
    public int LearnerTurnCount => _turns.Count(t => t.Role == ConversationRole.Learner);

    /// <summary>
    /// Whether the learner turn just added should be scored for pronunciation. Call this after the
    /// turn has been appended, so <see cref="LearnerTurnCount"/> already counts it. See
    /// <see cref="InitialAssessedTurns"/> and <see cref="AssessedTurnInterval"/> for the reasoning:
    /// assessment re-bills audio speech-to-text already charged for, so a conversation samples it
    /// rather than scoring every utterance.
    /// </summary>
    public bool ShouldAssessPronunciation =>
        LearnerTurnCount > 0 &&
        (LearnerTurnCount <= InitialAssessedTurns || LearnerTurnCount % AssessedTurnInterval == 0);

    /// <summary>
    /// True when the last turn is the learner's, i.e. recognition was persisted but the tutor never
    /// replied (a tutor timeout). A completed round always ends on a tutor turn, because
    /// <see cref="InsertTutorTurnAfterLastLearner"/> places the reply directly after the learner.
    /// Retrying the saved audio must continue this turn rather than appending a duplicate.
    /// </summary>
    public bool HasPendingLearnerTurn => LastTurn is { Role: ConversationRole.Learner };

    /// <summary>
    /// Cumulative engaged-speaking time: the sum of the (capped) gaps between consecutive learner
    /// utterances, so it approximates how long the learner has actively been conversing rather than
    /// wall-clock time with the page open. Advanced via <see cref="RecordSpokenActivity"/>.
    /// </summary>
    public TimeSpan SpokenTime { get; private set; }

    /// <summary>
    /// True once the learner has spoken for the full <see cref="MaxSpokenTime"/> this conversation -
    /// the cue to end the sitting (docs/development-guide.md rule 10).
    /// </summary>
    public bool HasReachedSpeakingLimit => SpokenTime >= MaxSpokenTime;

    /// <summary>
    /// Optional conversation topic. Either a curated code (e.g. "travel") or the English
    /// title of a vocabulary topic the learner just studied - the tutor uses it to anchor
    /// the conversation. Uzbek labels live in the frontend content store (docs/development-guide.md rule
    /// 11). Null means an open conversation.
    /// </summary>
    public string? Topic { get; }

    /// <summary>
    /// Target English words the learner just learned for <see cref="Topic"/> (from a linked
    /// vocabulary topic). The tutor encourages the learner to use them so speaking practice
    /// reinforces freshly learned vocabulary. Empty for a free conversation.
    /// </summary>
    public IReadOnlyList<string> FocusWords => _focusWords;

    public IReadOnlyList<ConversationTurn> Turns => _turns;

    /// <summary>
    /// Creates a session. <paramref name="sessionId"/> lets the caller mint the id up front so the
    /// energy spend can be booked against the same id before the session itself is persisted;
    /// pass null to have one generated.
    /// </summary>
    public static ConversationSession Start(
        Guid learnerId,
        CefrLevel level,
        string? topic = null,
        IEnumerable<string>? focusWords = null,
        Guid? vocabularyTopicId = null,
        string? learnerName = null,
        string? scenarioCode = null,
        SpeakingCurriculumContext? curriculumContext = null,
        Guid? sessionId = null)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");
        if (sessionId == Guid.Empty)
            throw new DomainException("Session id must not be empty.");

        var normalizedTopic = string.IsNullOrWhiteSpace(topic) ? null : topic.Trim();
        var normalizedWords = (focusWords ?? Enumerable.Empty<string>())
            .Where(w => !string.IsNullOrWhiteSpace(w))
            .Select(w => w.Trim())
            .ToList();
        var topicId = vocabularyTopicId == Guid.Empty ? null : vocabularyTopicId;
        var normalizedName = string.IsNullOrWhiteSpace(learnerName) ? null : learnerName.Trim();
        var normalizedScenario = string.IsNullOrWhiteSpace(scenarioCode) ? null : scenarioCode.Trim();
        return new ConversationSession(
            sessionId ?? Guid.NewGuid(), learnerId, level, normalizedTopic, normalizedWords, topicId,
            normalizedName, normalizedScenario, curriculumContext);
    }

    public ConversationSessionSnapshot ToSnapshot() => new(
        Id, LearnerId, Level, Topic, _focusWords.ToList(), VocabularyTopicId, LearnerName, ScenarioCode,
        SpokenTime, _lastActivityAt,
        _turns.Select(turn => new ConversationTurnSnapshot(
            turn.Role,
            turn.Text,
            turn.Pronunciation is null ? null : new PronunciationResultSnapshot(
                turn.Pronunciation.OverallScore,
                turn.Pronunciation.AccuracyScore,
                turn.Pronunciation.FluencyScore,
                turn.Pronunciation.CompletenessScore,
                turn.Pronunciation.Words.Select(word => new WordPronunciationSnapshot(
                    word.Word,
                    word.AccuracyScore,
                    word.ErrorType,
                    word.Phonemes.Select(phoneme => new PhonemePronunciationSnapshot(
                        phoneme.Phoneme, phoneme.AccuracyScore)).ToList(),
                    word.SpokenForm)).ToList()))).ToList(), CurriculumContext);

    public static ConversationSession Restore(ConversationSessionSnapshot snapshot)
    {
        var session = new ConversationSession(
            snapshot.Id, snapshot.LearnerId, snapshot.Level, snapshot.Topic, snapshot.FocusWords,
            snapshot.VocabularyTopicId, snapshot.LearnerName, snapshot.ScenarioCode, snapshot.CurriculumContext)
        {
            SpokenTime = snapshot.SpokenTime,
            _lastActivityAt = snapshot.LastActivityAt,
        };

        foreach (var turn in snapshot.Turns)
        {
            var pronunciation = turn.Pronunciation is null
                ? null
                : new PronunciationResult(
                    turn.Pronunciation.OverallScore,
                    turn.Pronunciation.AccuracyScore,
                    turn.Pronunciation.FluencyScore,
                    turn.Pronunciation.CompletenessScore,
                    turn.Pronunciation.Words.Select(word => new WordPronunciation(
                        word.Word,
                        word.AccuracyScore,
                        word.ErrorType,
                        word.Phonemes.Select(phoneme => new PhonemePronunciation(
                            phoneme.Phoneme, phoneme.AccuracyScore)).ToList(),
                        word.SpokenForm)).ToList());

            session._turns.Add(turn.Role == ConversationRole.Learner
                ? ConversationTurn.Learner(turn.Text, pronunciation)
                : ConversationTurn.Tutor(turn.Text));
        }

        return session;
    }

    public void SetCurriculumContext(SpeakingCurriculumContext? context) => CurriculumContext = context;

    /// <summary>
    /// Advances the engaged-speaking timer for a learner utterance happening at <paramref name="now"/>.
    /// The first utterance only sets the baseline (returns zero); each later utterance adds the gap
    /// since the previous one, capped at <see cref="MaxCountedGap"/>. Returns the time counted this
    /// call so the caller can credit it toward topic progress without double counting.
    /// </summary>
    public TimeSpan RecordSpokenActivity(DateTimeOffset now)
    {
        if (_lastActivityAt is { } last)
        {
            var gap = now - last;
            var counted = gap < TimeSpan.Zero
                ? TimeSpan.Zero
                : gap > MaxCountedGap ? MaxCountedGap : gap;
            SpokenTime += counted;
            _lastActivityAt = now;
            return counted;
        }

        _lastActivityAt = now;
        return TimeSpan.Zero;
    }

    public ConversationTurn AddTutorTurn(string text)
    {
        var turn = ConversationTurn.Tutor(text);
        _turns.Add(turn);
        return turn;
    }

    public ConversationTurn AddLearnerTurn(string text, PronunciationResult? pronunciation = null)
    {
        var turn = ConversationTurn.Learner(text, pronunciation);
        _turns.Add(turn);
        return turn;
    }

    public void AttachPronunciationToLastLearnerTurn(PronunciationResult pronunciation)
    {
        var learnerIndex = _turns.FindLastIndex(turn => turn.Role == ConversationRole.Learner);
        if (learnerIndex < 0)
            throw new InvalidOperationException("The conversation has no learner turn.");

        _turns[learnerIndex] = ConversationTurn.Learner(_turns[learnerIndex].Text, pronunciation);
    }

    public void InsertTutorTurnAfterLastLearner(string text)
    {
        var learnerIndex = _turns.FindLastIndex(turn => turn.Role == ConversationRole.Learner);
        if (learnerIndex < 0)
            throw new InvalidOperationException("The conversation has no learner turn.");

        _turns.Insert(learnerIndex + 1, ConversationTurn.Tutor(text));
    }

    public ConversationTurn? LastTurn => _turns.Count == 0 ? null : _turns[^1];

    /// <summary>
    /// Updates the name the tutor addresses the learner by. Used when the learner sets or corrects
    /// their preferred name mid-conversation (e.g. via the name prompt that appears when speech-to-text
    /// could not capture a spoken introduction) so the tutor starts using it on the next turn without
    /// restarting the session. Blank clears it back to null (the tutor then uses no name).
    /// </summary>
    public void SetLearnerName(string? name)
    {
        LearnerName = string.IsNullOrWhiteSpace(name) ? null : name.Trim();
    }
}
