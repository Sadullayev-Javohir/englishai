using Domain.Common;

namespace Domain.Competition;

/// <summary>
/// A single question slide in a competition, generated when the competition is built from the
/// selected vocabulary/grammar topics. The question + options are cached on the slide so scoring
/// needs no round-trip to the source topic and the game is reproducible/cheap.
/// </summary>
public sealed class CompetitionSlide
{
    // Parameterless ctor for EF Core materialization.
    private CompetitionSlide()
    {
        QuestionText = null!;
        Options = null!;
    }

    private CompetitionSlide(
        int order, SlideSourceType sourceType, Guid sourceTopicId, string questionText,
        IReadOnlyList<string> options, int correctOptionIndex, int points)
    {
        Id = Guid.NewGuid();
        Order = order;
        SourceType = sourceType;
        SourceTopicId = sourceTopicId;
        QuestionText = questionText;
        Options = options;
        CorrectOptionIndex = correctOptionIndex;
        Points = points;
    }

    public Guid Id { get; private set; }
    public int Order { get; private set; }
    public SlideSourceType SourceType { get; private set; }
    public Guid SourceTopicId { get; private set; }
    public string QuestionText { get; private set; }
    public IReadOnlyList<string> Options { get; private set; }
    public int CorrectOptionIndex { get; private set; }
    /// <summary>Max points obtainable on this slide (base, before speed bonus scaling on submit).</summary>
    public int Points { get; private set; }

    public static CompetitionSlide Create(
        int order, SlideSourceType sourceType, Guid sourceTopicId, string questionText,
        IReadOnlyList<string> options, int correctOptionIndex, int points)
    {
        if (string.IsNullOrWhiteSpace(questionText))
            throw new DomainException("Slide question must not be empty.");
        if (options is null || options.Count < 2)
            throw new DomainException("A slide needs at least 2 options.");
        if (options.Any(string.IsNullOrWhiteSpace))
            throw new DomainException("Slide options must not be empty.");
        if (correctOptionIndex < 0 || correctOptionIndex >= options.Count)
            throw new DomainException("Slide correct-option index is out of range.");
        if (points < 1)
            throw new DomainException("Slide points must be at least 1.");

        return new CompetitionSlide(
            order, sourceType, sourceTopicId, questionText.Trim(),
            options.Select(o => o.Trim()).ToList(), correctOptionIndex, points);
    }

    public bool IsCorrect(int selectedOptionIndex) => selectedOptionIndex == CorrectOptionIndex;
}
