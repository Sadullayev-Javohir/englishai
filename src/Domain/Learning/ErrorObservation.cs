namespace Domain.Learning;

/// <summary>
/// One observed learner error feeding the error heatmap (PROJECT-SPEC C.7 / G.2).
/// Modules report errors with a structured <see cref="ErrorCategory"/> code; the
/// human-readable Uzbek explanation is resolved from templates elsewhere.
/// </summary>
public sealed class ErrorObservation
{
    // Parameterless ctor for EF Core materialization.
    private ErrorObservation()
    {
    }

    public ErrorObservation(
        ErrorCategory category,
        SkillType skill,
        DateTimeOffset occurredAt,
        string? source = null,
        Guid? sourceId = null,
        string? prompt = null,
        string? learnerAnswer = null,
        string? expectedAnswer = null,
        string? explanation = null)
    {
        Id = Guid.NewGuid();
        Category = category;
        Skill = skill;
        OccurredAt = occurredAt;
        Source = Normalize(source, 64);
        SourceId = sourceId;
        Prompt = Normalize(prompt, 500);
        LearnerAnswer = Normalize(learnerAnswer, 500);
        ExpectedAnswer = Normalize(expectedAnswer, 500);
        Explanation = Normalize(explanation, 1000);
    }

    public Guid Id { get; private set; }
    public ErrorCategory Category { get; private set; }

    /// <summary>The skill the error was made in (e.g. an article slip while Speaking).</summary>
    public SkillType Skill { get; private set; }

    public DateTimeOffset OccurredAt { get; private set; }

    public string? Source { get; private set; }
    public Guid? SourceId { get; private set; }
    public string? Prompt { get; private set; }
    public string? LearnerAnswer { get; private set; }
    public string? ExpectedAnswer { get; private set; }
    public string? Explanation { get; private set; }

    private static string? Normalize(string? value, int maxLength)
    {
        if (string.IsNullOrWhiteSpace(value)) return null;
        var normalized = value.Trim();
        return normalized.Length <= maxLength ? normalized : normalized[..maxLength];
    }
}
