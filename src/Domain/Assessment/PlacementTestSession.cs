using Domain.Common;

namespace Domain.Assessment;

/// <summary>
/// Aggregate root for a single learner's adaptive CEFR placement test.
///
/// Adaptive (Computer Adaptive Testing) rule per PROJECT-SPEC G.1:
///   * three consecutive correct answers -> the next question gets one level harder;
///   * two consecutive wrong answers     -> the next question gets one level easier.
/// Difficulty is clamped to the A1..C2 range. The streak counters reset whenever the
/// difficulty changes or the stage advances.
/// </summary>
public sealed class PlacementTestSession
{
    /// <summary>Difficulty the test starts at - a low-middle entry point that
    /// leaves room to adapt in either direction.</summary>
    public const CefrLevel DefaultStartingDifficulty = CefrLevel.A2;

    public const int ConsecutiveCorrectToLevelUp = 3;
    public const int ConsecutiveWrongToLevelDown = 2;

    /// <summary>
    /// How many distinct integrity incidents a session tolerates before it is invalidated.
    /// Deliberately not 2: the browser signals we monitor also fire for things that are not
    /// cheating (an incoming call, the notification shade, the Android soft keyboard dropping
    /// fullscreen on the Writing task, the microphone permission prompt on the Speaking task).
    /// At two strikes a learner who answered all 21 receptive items lost the whole test on the
    /// last two questions - measured in production, that was the single biggest reason accounts
    /// never got a CEFR level. Four still stops deliberate tab-switching.
    /// </summary>
    public const int ViolationsBeforeInvalidation = 4;

    /// <summary>How many items are asked in each stage. The four receptive stages use
    /// adaptive multiple-choice items; Writing and Speaking use one productive task each.</summary>
    public static readonly IReadOnlyDictionary<TestStage, int> QuestionsPerStage =
        new Dictionary<TestStage, int>
        {
            [TestStage.Vocabulary] = 6,
            [TestStage.Grammar] = 6,
            [TestStage.Listening] = 5,
            [TestStage.Reading] = 4,
            [TestStage.Writing] = 1,
            [TestStage.Speaking] = 1
        };

    /// <summary>The stages graded from a free-response task (free text / recorded audio)
    /// rather than multiple-choice answers.</summary>
    public static readonly IReadOnlySet<TestStage> ProductiveStages =
        new HashSet<TestStage> { TestStage.Writing, TestStage.Speaking };

    private static readonly TestStage[] FullStageOrder =
    {
        TestStage.Vocabulary,
        TestStage.Grammar,
        TestStage.Listening,
        TestStage.Reading,
        TestStage.Writing,
        TestStage.Speaking
    };

    private readonly List<AnswerRecord> _answers = new();
    private readonly List<ProductiveResult> _productiveResults = new();
    private readonly List<TestStage> _stageSequence;
    private readonly HashSet<Guid> _integrityIncidentIds = new();

    private int _consecutiveCorrect;
    private int _consecutiveWrong;
    private int _questionsAnsweredInStage;
    private Guid? _currentItemId;

    private PlacementTestSession(
        Guid id,
        Guid learnerId,
        IEnumerable<TestStage> stageSequence,
        CefrLevel startingDifficulty)
    {
        Id = id;
        LearnerId = learnerId;
        _stageSequence = stageSequence.ToList();
        CurrentStage = _stageSequence[0];
        CurrentDifficulty = startingDifficulty;
        StartingDifficulty = startingDifficulty;
    }

    public Guid Id { get; }
    public Guid LearnerId { get; }
    public TestStage CurrentStage { get; private set; }
    public CefrLevel CurrentDifficulty { get; private set; }
    public CefrLevel StartingDifficulty { get; private set; }
    public bool IsCompleted { get; private set; }
    public IReadOnlyList<AnswerRecord> Answers => _answers;
    public IReadOnlyList<ProductiveResult> ProductiveResults => _productiveResults;
    public int TotalQuestionsAnswered => _answers.Count;
    public Guid? CurrentItemId => _currentItemId;
    public int QuestionsAnsweredInStage => _questionsAnsweredInStage;
    public int CurrentStageNumber => _stageSequence.IndexOf(CurrentStage) + 1;
    public int StageCount => _stageSequence.Count;
    public int CompletedItemCount => _answers.Count + _productiveResults.Count;
    public int TotalItemCount => _stageSequence.Sum(stage => QuestionsPerStage[stage]);
    public CefrLevel? FinalizedLevel { get; private set; }
    public bool PlacementApplied { get; private set; }
    public int IntegrityViolationCount { get; private set; }
    public bool IsIntegrityInvalidated { get; private set; }

    /// <summary>True when the current stage is graded from a free-response task
    /// (Writing/Speaking) rather than multiple-choice questions.</summary>
    public bool IsCurrentStageProductive => ProductiveStages.Contains(CurrentStage);

    /// <summary>
    /// Starts a new placement session. Speaking is optional (PROJECT-SPEC G.1);
    /// when excluded the test ends after the Reading stage.
    /// </summary>
    public static PlacementTestSession Start(
        Guid learnerId,
        bool includeSpeaking = true,
        CefrLevel startingDifficulty = DefaultStartingDifficulty)
    {
        if (learnerId == Guid.Empty)
            throw new DomainException("Learner id must not be empty.");

        var stages = includeSpeaking
            ? FullStageOrder
            : FullStageOrder.Where(s => s != TestStage.Speaking).ToArray();

        return new PlacementTestSession(Guid.NewGuid(), learnerId, stages, startingDifficulty);
    }

    /// <summary>Registers the exact item returned to the learner. A submission is valid
    /// only for this item, which prevents cross-stage IDs, replay and answer injection.</summary>
    public void ServeItem(Guid itemId)
    {
        EnsureIntegrity();
        if (IsCompleted)
            throw new DomainException("Cannot serve an item: the placement test is already completed.");
        if (itemId == Guid.Empty)
            throw new DomainException("Item id must not be empty.");

        _currentItemId = itemId;
    }

    /// <summary>
    /// Records the outcome of the current question, adapts the difficulty, and
    /// advances the stage (or completes the test) when the stage's question quota
    /// is reached.
    /// </summary>
    public void RecordAnswer(Guid questionId, bool isCorrect)
    {
        EnsureIntegrity();
        if (IsCompleted)
            throw new DomainException("Cannot record an answer: the placement test is already completed.");
        if (IsCurrentStageProductive)
            throw new DomainException($"The {CurrentStage} stage is graded from a free-response task; use RecordProductiveResult.");
        if (questionId == Guid.Empty)
            throw new DomainException("Question id must not be empty.");
        EnsureCurrentItem(questionId);

        _currentItemId = null;
        _answers.Add(new AnswerRecord(questionId, CurrentStage, CurrentDifficulty, isCorrect));
        _questionsAnsweredInStage++;

        if (isCorrect)
        {
            _consecutiveCorrect++;
            _consecutiveWrong = 0;
            if (_consecutiveCorrect >= ConsecutiveCorrectToLevelUp)
            {
                CurrentDifficulty = CurrentDifficulty.StepUp();
                _consecutiveCorrect = 0;
            }
        }
        else
        {
            _consecutiveWrong++;
            _consecutiveCorrect = 0;
            if (_consecutiveWrong >= ConsecutiveWrongToLevelDown)
            {
                CurrentDifficulty = CurrentDifficulty.StepDown();
                _consecutiveWrong = 0;
            }
        }

        if (_questionsAnsweredInStage >= QuestionsPerStage[CurrentStage])
        {
            AdvanceStage();
        }
    }

    /// <summary>
    /// Records the graded 0-100 outcome of the current productive stage's task
    /// (Writing or Speaking) and advances to the next stage (or completes the test).
    /// Each productive stage has a single task, so recording one always advances.
    /// </summary>
    public void RecordProductiveResult(Guid taskId, int score, CefrLevel taskDifficulty)
    {
        EnsureIntegrity();
        if (IsCompleted)
            throw new DomainException("Cannot record a result: the placement test is already completed.");
        if (!IsCurrentStageProductive)
            throw new DomainException($"The {CurrentStage} stage is multiple-choice; use RecordAnswer.");
        if (score is < 0 or > 100)
            throw new DomainException("Productive score must be between 0 and 100.");
        EnsureCurrentItem(taskId);

        _currentItemId = null;
        var cappedScore = PlacementScoring.CapProductiveScore(score, taskDifficulty);
        _productiveResults.Add(new ProductiveResult(CurrentStage, cappedScore, taskDifficulty));
        CurrentDifficulty = CefrLevelExtensions.FromScore(cappedScore);
        AdvanceStage();
    }

    /// <summary>Completes only the optional final Speaking stage. Onboarding placement
    /// never calls this path; it exists for explicitly configured level-exit tests.</summary>
    public void CompleteWithoutOptionalSpeaking()
    {
        EnsureIntegrity();
        if (IsCompleted)
            return;
        if (CurrentStage != TestStage.Speaking || _currentItemId is not null)
            throw new DomainException("Only an unserved optional Speaking stage can be skipped.");

        IsCompleted = true;
    }

    /// <summary>
    /// Captures the session's full state as an immutable snapshot for durable storage
    /// (see <see cref="PlacementSessionSnapshot"/>). The collections are copied so the
    /// snapshot is decoupled from this aggregate's mutable internals.
    /// </summary>
    public PlacementSessionSnapshot ToSnapshot() => new(
        Id,
        LearnerId,
        _stageSequence.ToList(),
        CurrentStage,
        CurrentDifficulty,
        StartingDifficulty,
        IsCompleted,
        _consecutiveCorrect,
        _consecutiveWrong,
        _questionsAnsweredInStage,
        _answers.ToList(),
        _productiveResults.ToList(),
        _currentItemId,
        FinalizedLevel,
        IntegrityViolationCount,
        IsIntegrityInvalidated,
        _integrityIncidentIds.ToList(),
        PlacementApplied);

    /// <summary>
    /// Rehydrates a session from a previously captured <see cref="PlacementSessionSnapshot"/>.
    /// Used by the durable store to resume an in-flight test after it has been read back from
    /// Redis (or another backing store).
    /// </summary>
    public static PlacementTestSession Restore(PlacementSessionSnapshot snapshot)
    {
        var session = new PlacementTestSession(
            snapshot.Id,
            snapshot.LearnerId,
            snapshot.StageSequence,
            snapshot.StartingDifficulty ?? snapshot.CurrentDifficulty)
        {
            CurrentStage = snapshot.CurrentStage,
            CurrentDifficulty = snapshot.CurrentDifficulty,
            IsCompleted = snapshot.IsCompleted,
            FinalizedLevel = snapshot.FinalizedLevel,
            PlacementApplied = snapshot.PlacementApplied,
            IntegrityViolationCount = snapshot.IntegrityViolationCount,
            IsIntegrityInvalidated = snapshot.IsIntegrityInvalidated,
        };

        session._consecutiveCorrect = snapshot.ConsecutiveCorrect;
        session._consecutiveWrong = snapshot.ConsecutiveWrong;
        session._questionsAnsweredInStage = snapshot.QuestionsAnsweredInStage;
        session._currentItemId = snapshot.CurrentItemId;
        session._answers.AddRange(snapshot.Answers);
        session._productiveResults.AddRange(snapshot.ProductiveResults);
        session._integrityIncidentIds.UnionWith(snapshot.IntegrityIncidentIds ?? Array.Empty<Guid>());

        return session;
    }

    /// <summary>Computes the final result. The test must be completed first.</summary>
    public PlacementResult Finalize()
    {
        EnsureIntegrity();
        if (!IsCompleted)
            throw new DomainException("Cannot finalize: the placement test is not completed yet.");

        return PlacementScoring.Calculate(_answers, _productiveResults);
    }

    public void MarkExitTestFinalized(CefrLevel level)
    {
        EnsureIntegrity();
        if (!IsCompleted)
            throw new DomainException("Cannot mark an incomplete placement test as finalized.");

        FinalizedLevel ??= level;
    }

    public void MarkPlacementApplied()
    {
        EnsureIntegrity();
        if (!IsCompleted)
            throw new DomainException("Cannot apply an incomplete placement test.");
        PlacementApplied = true;
    }


    public bool RecordIntegrityViolation(Guid incidentId)
    {
        if (incidentId == Guid.Empty)
            throw new DomainException("Integrity incident id must not be empty.");
        if (!_integrityIncidentIds.Add(incidentId))
            return false;

        IntegrityViolationCount++;
        if (IntegrityViolationCount >= ViolationsBeforeInvalidation)
            IsIntegrityInvalidated = true;
        return true;
    }

    public void EnsureIntegrity()
    {
        if (IsIntegrityInvalidated)
            throw new DomainException("This placement session was invalidated because the secure test rules were violated.");
    }

    private void EnsureCurrentItem(Guid itemId)
    {
        if (_currentItemId is null)
            throw new DomainException("No placement item is currently awaiting an answer.");
        if (_currentItemId != itemId)
            throw new DomainException("The submitted item does not match the current placement item.");
    }

    private void AdvanceStage()
    {
        var currentIndex = _stageSequence.IndexOf(CurrentStage);
        if (currentIndex == _stageSequence.Count - 1)
        {
            IsCompleted = true;
            return;
        }

        CurrentStage = _stageSequence[currentIndex + 1];
        _questionsAnsweredInStage = 0;
        _consecutiveCorrect = 0;
        _consecutiveWrong = 0;
        // Difficulty deliberately carries forward into the next stage as a sensible
        // starting estimate of the learner's level.
    }
}
