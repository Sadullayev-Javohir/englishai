using Domain.Common;
using Domain.Learning;

namespace Domain.Grammar;

/// <summary>
/// The lesson's final step (PROJECT-SPEC G.2 step 5): a short task that takes the grammar
/// rule out of isolation and into a real skill by asking the learner to use it in the
/// Speaking or Writing module. It is a guided prompt, not auto-graded here - the actual
/// production is assessed by the target module. The English prompt is content data.
/// </summary>
public sealed class GrammarApplicationTask
{
    // Parameterless ctor for EF Core materialization.
    private GrammarApplicationTask()
    {
        Prompt = null!;
    }

    private GrammarApplicationTask(SkillType targetSkill, string prompt)
    {
        Id = Guid.NewGuid();
        TargetSkill = targetSkill;
        Prompt = prompt;
    }

    public Guid Id { get; private set; }

    /// <summary>The module the learner should use to complete the task (Speaking or Writing).</summary>
    public SkillType TargetSkill { get; private set; }

    /// <summary>The English instruction for the production task.</summary>
    public string Prompt { get; private set; }

    public static GrammarApplicationTask Create(SkillType targetSkill, string prompt)
    {
        if (targetSkill is not (SkillType.Speaking or SkillType.Writing))
            throw new DomainException("A grammar application task must target Speaking or Writing.");
        if (string.IsNullOrWhiteSpace(prompt))
            throw new DomainException("Application task prompt must not be empty.");

        return new GrammarApplicationTask(targetSkill, prompt.Trim());
    }
}
