using Domain.Common;
using Domain.Learning;

namespace Domain.Vocabulary;

/// <summary>
/// One module's best result toward completing a <see cref="TopicCompletionRecord"/> (PROJECT-SPEC
/// K.5). Each of the six learning modules (Vocabulary, Grammar, Reading, Listening, Speaking,
/// Writing) contributes a single 0-100 score; the highest score the learner has achieved for that
/// module is kept, so once a module is passed it stays passed even if a later attempt scores lower.
/// </summary>
public sealed class TopicModuleScore
{
    // Parameterless ctor for EF Core materialization.
    private TopicModuleScore()
    {
    }

    internal TopicModuleScore(SkillType module, int score, DateTimeOffset achievedAt)
    {
        Module = module;
        Score = score;
        AchievedAt = achievedAt;
    }

    /// <summary>Which of the six learning modules this score belongs to.</summary>
    public SkillType Module { get; private set; }

    /// <summary>The learner's best score for this module, 0-100.</summary>
    public int Score { get; private set; }

    /// <summary>When the current best score was achieved (PROJECT-SPEC K.5 <c>CompletedAt</c>).</summary>
    public DateTimeOffset AchievedAt { get; private set; }

    /// <summary>Replaces the stored result only when <paramref name="score"/> beats the best so far.</summary>
    internal void RecordIfBetter(int score, DateTimeOffset achievedAt)
    {
        if (score <= Score)
            return;

        Score = score;
        AchievedAt = achievedAt;
    }

    internal static int Validate(int score)
    {
        if (score is < 0 or > 100)
            throw new DomainException("Module score must be between 0 and 100.");
        return score;
    }
}
