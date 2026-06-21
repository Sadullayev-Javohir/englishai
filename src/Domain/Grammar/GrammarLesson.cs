using Domain.Assessment;
using Domain.Common;
using Domain.Learning;

namespace Domain.Grammar;

/// <summary>
/// Aggregate root for a single grammar topic taught with the fixed 5-step lesson format
/// (PROJECT-SPEC G.2): contextual intro → rule explanation → recognition exercises →
/// active-use exercises → a Speaking/Writing application task. The lesson is tagged with a
/// CEFR <see cref="Level"/> and an <see cref="ErrorCategory"/> so it slots into the
/// Uzbek-learner difficulty priority order (articles first, then tense, ...) and so a
/// learner's wrong answers feed the error heatmap (Qism C.7 integration). English prompts
/// are authored content; all learner-facing Uzbek text is resolved from vetted templates
/// (docs/development-guide.md rule 11) via the explanation/hint codes - never free-generated here.
/// </summary>
public sealed class GrammarLesson
{
    private readonly List<GrammarExercise> _exercises = new();
    private readonly List<GrammarApplicationTask> _applicationTasks = new();
    private readonly List<GrammarExample> _examples = new();
    private readonly List<GrammarCommonMistake> _commonMistakes = new();
    private readonly List<string> _curatedFormulas = new();
    private readonly List<GrammarCuratedRule> _curatedRules = new();

    // Parameterless ctor for EF Core materialization.
    private GrammarLesson()
    {
        Topic = null!;
        ContextIntro = null!;
        ExplanationCode = null!;
    }

    private GrammarLesson(
        string topic,
        ErrorCategory category,
        CefrLevel level,
        string contextIntro,
        string explanationCode,
        DateTimeOffset now)
    {
        Id = Guid.NewGuid();
        Topic = topic;
        Category = category;
        Level = level;
        ContextIntro = contextIntro;
        ExplanationCode = explanationCode;
        CreatedAt = now;
    }

    public Guid Id { get; private set; }

    /// <summary>
    /// The shared learning-spine topic this grammar lesson teaches, when the lesson was created
    /// for a topic (the topic-scoped lazy-fill path). Null for legacy curated lessons organised by
    /// error priority.
    /// </summary>
    public Guid? VocabularyTopicId { get; private set; }

    /// <summary>
    /// The grammar point taught in this lesson (a kebab-case focus code from the topic's level
    /// syllabus, e.g. "past-simple"). Set on the topic-scoped path; null for legacy lessons.
    /// </summary>
    public string? GrammarFocusCode { get; private set; }

    /// <summary>The grammar topic shown in the catalog and lesson header (English).</summary>
    public string Topic { get; private set; }

    /// <summary>
    /// The error category this topic addresses. Doubles as the catalog priority order
    /// (lower enum value = higher Uzbek-learner priority) and the heatmap bucket a wrong
    /// answer is reported to.
    /// </summary>
    public ErrorCategory Category { get; private set; }

    /// <summary>The curated CEFR level of the lesson.</summary>
    public CefrLevel Level { get; private set; }

    /// <summary>
    /// Step 1: a short English text/dialogue showing the rule in real use rather than in
    /// isolation (PROJECT-SPEC G.2 step 1).
    /// </summary>
    public string ContextIntro { get; private set; }

    /// <summary>
    /// Step 2 (legacy curated path): content-store code resolving to the vetted Uzbek rule
    /// explanation (docs/development-guide.md rule 11). Empty on the topic-scoped path, where the explanation is
    /// generated in English (see <see cref="Explanation"/>).
    /// </summary>
    public string ExplanationCode { get; private set; }

    /// <summary>
    /// Step 2 (topic-scoped path): the generated English rule explanation (the immersion teaching
    /// note - the sanctioned dynamic path of rule 11, the same as a reading question's explanation).
    /// Null on the legacy path, where the Uzbek explanation is resolved from <see cref="ExplanationCode"/>.
    /// </summary>
    public string? Explanation { get; private set; }

    /// <summary>
    /// Step 2 companion: short example sentences showing the focus in use (English + a short Uzbek
    /// meaning). Generated alongside the lesson so the "Misollar" step always has content (rules 8, 11).
    /// </summary>
    public IReadOnlyList<GrammarExample> Examples => _examples;

    /// <summary>
    /// Step 2 companion: the common mistakes Uzbek learners make with this focus, written in Uzbek.
    /// Generated alongside the lesson so the "Xatolar" step always has content (rule 11).
    /// </summary>
    public IReadOnlyList<GrammarCommonMistake> CommonMistakes => _commonMistakes;
    public string? CuratedTitleUz { get; private set; }
    public string? CuratedSummaryUz { get; private set; }
    public IReadOnlyList<string> CuratedFormulas => _curatedFormulas;
    public IReadOnlyList<GrammarCuratedRule> CuratedRules => _curatedRules;

    /// <summary>Pending until the five steps are generated and cached (topic-scoped path).</summary>
    public GrammarLessonStatus Status { get; private set; } = GrammarLessonStatus.Filled;

    /// <summary>True once the lesson's five steps have been generated and cached.</summary>
    public bool IsFilled => Status == GrammarLessonStatus.Filled;

    public DateTimeOffset CreatedAt { get; private set; }

    /// <summary>Steps 3-4: the auto-gradable recognition and active-use exercises.</summary>
    public IReadOnlyList<GrammarExercise> Exercises => _exercises;

    /// <summary>Step 5: the guided Speaking/Writing application task(s).</summary>
    public IReadOnlyList<GrammarApplicationTask> ApplicationTasks => _applicationTasks;

    /// <summary>
    /// Builds a fully curated lesson (all five steps known up front), as used by the
    /// seeded grammar catalog.
    /// </summary>
    public static GrammarLesson Curate(
        string topic,
        ErrorCategory category,
        CefrLevel level,
        string contextIntro,
        string explanationCode,
        IEnumerable<GrammarExercise> exercises,
        IEnumerable<GrammarApplicationTask> applicationTasks,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new DomainException("Grammar topic must not be empty.");
        if (string.IsNullOrWhiteSpace(contextIntro))
            throw new DomainException("Context intro must not be empty.");
        if (string.IsNullOrWhiteSpace(explanationCode))
            throw new DomainException("Explanation code must not be empty.");

        var lesson = new GrammarLesson(
            topic.Trim(), category, level, contextIntro.Trim(), explanationCode.Trim(), now);

        foreach (var exercise in exercises)
        {
            if (exercise is null)
                throw new DomainException("Exercise must not be null.");
            lesson._exercises.Add(exercise);
        }

        foreach (var task in applicationTasks)
        {
            if (task is null)
                throw new DomainException("Application task must not be null.");
            lesson._applicationTasks.Add(task);
        }

        if (lesson._exercises.Count == 0)
            throw new DomainException("A grammar lesson needs at least one exercise.");

        return lesson;
    }

    /// <summary>
    /// Creates a pending grammar lesson bound to a learning-spine topic. Its five steps (context
    /// intro, English rule explanation, recognition + active-use exercises and a Speaking/Writing
    /// application task) are generated by the LLM on first open and cached via
    /// <see cref="FillContent"/> - the same lazy-fill pattern as a reading lesson (rules 8, 10). The
    /// <paramref name="category"/> (derived from the grammar focus) feeds the error heatmap (C.7).
    /// </summary>
    public static GrammarLesson ForTopic(
        Guid vocabularyTopicId,
        string topic,
        ErrorCategory category,
        CefrLevel level,
        string grammarFocusCode,
        DateTimeOffset now)
    {
        if (vocabularyTopicId == Guid.Empty)
            throw new DomainException("A topic-scoped lesson needs a vocabulary topic id.");
        if (string.IsNullOrWhiteSpace(topic))
            throw new DomainException("Grammar topic must not be empty.");
        if (string.IsNullOrWhiteSpace(grammarFocusCode))
            throw new DomainException("Grammar focus code must not be empty.");

        return new GrammarLesson(topic.Trim(), category, level, string.Empty, string.Empty, now)
        {
            VocabularyTopicId = vocabularyTopicId,
            GrammarFocusCode = grammarFocusCode.Trim(),
            Status = GrammarLessonStatus.Pending,
        };
    }

    /// <summary>
    /// Creates a PENDING, admin-curated lesson directly (the /admin/grammar management path).
    /// Unlike <see cref="ForTopic"/> it is not bound to a learning-spine topic and carries no
    /// generated content yet - the five steps are filled later. Mirrors the ForTopic guards:
    /// the topic must be non-empty; the category and level are enum-typed so always valid.
    /// </summary>
    public static GrammarLesson CreateManual(
        string topic,
        ErrorCategory category,
        CefrLevel level,
        DateTimeOffset now)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new DomainException("Grammar topic must not be empty.");

        return new GrammarLesson(topic.Trim(), category, level, string.Empty, string.Empty, now)
        {
            VocabularyTopicId = null,
            GrammarFocusCode = null,
            ContextIntro = string.Empty,
            Explanation = null,
            Status = GrammarLessonStatus.Pending,
        };
    }

    /// <summary>
    /// Admin edit of a lesson's catalog metadata (topic, error category and CEFR level).
    /// </summary>
    public void AdminUpdate(string topic, ErrorCategory category, CefrLevel level)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new DomainException("Grammar topic must not be empty.");
        Topic = topic.Trim();
        Category = category;
        Level = level;
    }

    public void AdminReplace(
        string topic,
        ErrorCategory category,
        CefrLevel level,
        GrammarLessonStatus status,
        Guid? vocabularyTopicId,
        string? grammarFocusCode,
        string? contextIntro,
        string? explanation,
        IEnumerable<GrammarExample> examples,
        IEnumerable<GrammarCommonMistake> commonMistakes,
        IEnumerable<GrammarExercise> exercises,
        IEnumerable<GrammarApplicationTask> applicationTasks)
    {
        if (string.IsNullOrWhiteSpace(topic))
            throw new DomainException("Grammar topic must not be empty.");

        var exerciseList = exercises?.ToList() ?? throw new DomainException("Exercises must not be null.");
        if (status == GrammarLessonStatus.Filled && exerciseList.Count == 0)
            throw new DomainException("A filled grammar lesson must have at least one exercise.");
        if (status == GrammarLessonStatus.Filled && string.IsNullOrWhiteSpace(contextIntro))
            throw new DomainException("A filled grammar lesson needs a context intro.");
        if (status == GrammarLessonStatus.Filled && string.IsNullOrWhiteSpace(explanation))
            throw new DomainException("A filled grammar lesson needs a rule explanation.");

        Topic = topic.Trim();
        Category = category;
        Level = level;
        Status = status;
        VocabularyTopicId = vocabularyTopicId;
        GrammarFocusCode = string.IsNullOrWhiteSpace(grammarFocusCode) ? null : grammarFocusCode.Trim();
        ContextIntro = string.IsNullOrWhiteSpace(contextIntro) ? string.Empty : contextIntro.Trim();
        Explanation = string.IsNullOrWhiteSpace(explanation) ? null : explanation.Trim();

        _examples.Clear();
        _examples.AddRange(examples ?? Enumerable.Empty<GrammarExample>());
        _commonMistakes.Clear();
        _commonMistakes.AddRange(commonMistakes ?? Enumerable.Empty<GrammarCommonMistake>());
        _exercises.Clear();
        _exercises.AddRange(exerciseList);
        _applicationTasks.Clear();
        _applicationTasks.AddRange(applicationTasks ?? Enumerable.Empty<GrammarApplicationTask>());
    }

    public void ReplaceCuratedRuleContent(
        string? titleUz,
        string? summaryUz,
        IEnumerable<string> formulas,
        IEnumerable<GrammarCuratedRule> rules)
    {
        CuratedTitleUz = string.IsNullOrWhiteSpace(titleUz) ? null : titleUz.Trim();
        CuratedSummaryUz = string.IsNullOrWhiteSpace(summaryUz) ? null : summaryUz.Trim();
        _curatedFormulas.Clear();
        _curatedFormulas.AddRange((formulas ?? Enumerable.Empty<string>()).Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()));
        _curatedRules.Clear();
        _curatedRules.AddRange(rules ?? Enumerable.Empty<GrammarCuratedRule>());
    }

    /// <summary>
    /// Stores the generated five steps and marks the lesson filled. Called once after the LLM
    /// produces the content; later reads serve the cache. The explanation is the English rule
    /// note (immersion teaching content), and each exercise carries its own English explanation.
    /// The example sentences and common Uzbek-learner mistakes accompany the rule so the "Misollar"
    /// and "Xatolar" steps always have content (rules 8, 11).
    /// </summary>
    public void FillContent(
        string contextIntro,
        string explanation,
        IEnumerable<GrammarExample> examples,
        IEnumerable<GrammarCommonMistake> commonMistakes,
        IEnumerable<GrammarExercise> exercises,
        IEnumerable<GrammarApplicationTask> applicationTasks)
    {
        if (string.IsNullOrWhiteSpace(contextIntro))
            throw new DomainException("Context intro must not be empty.");
        if (string.IsNullOrWhiteSpace(explanation))
            throw new DomainException("Rule explanation must not be empty.");

        var exerciseList = exercises?.ToList() ?? throw new DomainException("Exercises must not be null.");
        if (exerciseList.Count == 0)
            throw new DomainException("A filled grammar lesson must have at least one exercise.");

        ContextIntro = contextIntro.Trim();
        Explanation = explanation.Trim();

        _examples.Clear();
        foreach (var example in examples ?? Enumerable.Empty<GrammarExample>())
            _examples.Add(example ?? throw new DomainException("Example must not be null."));

        _commonMistakes.Clear();
        foreach (var mistake in commonMistakes ?? Enumerable.Empty<GrammarCommonMistake>())
            _commonMistakes.Add(mistake ?? throw new DomainException("Common mistake must not be null."));

        _exercises.Clear();
        foreach (var exercise in exerciseList)
            _exercises.Add(exercise ?? throw new DomainException("Exercise must not be null."));

        _applicationTasks.Clear();
        foreach (var task in applicationTasks ?? Enumerable.Empty<GrammarApplicationTask>())
            _applicationTasks.Add(task ?? throw new DomainException("Application task must not be null."));

        Status = GrammarLessonStatus.Filled;
    }

    /// <summary>
    /// Grades a learner's exercise answers. Exercises with no submitted answer count as
    /// incorrect, so the score reflects the whole lesson (PROJECT-SPEC G.2). The result's
    /// wrong outcomes carry the lesson <see cref="Category"/> so the caller can feed the
    /// error heatmap.
    /// </summary>
    public GrammarExerciseResult GradeExercises(IReadOnlyDictionary<Guid, int> answers)
    {
        if (_exercises.Count == 0)
            throw new DomainException("This lesson has no exercises.");

        var outcomes = new List<GrammarExerciseOutcome>(_exercises.Count);
        foreach (var exercise in _exercises)
        {
            var hasAnswer = answers.TryGetValue(exercise.Id, out var selected);
            if (!hasAnswer)
                selected = -1;

            outcomes.Add(new GrammarExerciseOutcome(
                exercise.Id,
                selected,
                exercise.CorrectOptionIndex,
                hasAnswer && exercise.IsCorrect(selected),
                exercise.HintCode,
                exercise.Explanation));
        }

        return new GrammarExerciseResult(
            Id, _exercises.Count, outcomes.Count(o => o.IsCorrect), outcomes);
    }
}
