using Domain.Common;

namespace Domain.Grammar;

/// <summary>
/// A short example sentence for a generated grammar lesson (PROJECT-SPEC G.2 step 2 companion):
/// the exact English sentence showing the focus in use, paired with a short Uzbek meaning. The
/// English is target-language teaching content; the Uzbek is learner-facing and vetted/templated
/// (docs/development-guide.md rule 11). Owned by <see cref="GrammarLesson"/> (EF Core <c>OwnsMany</c>), so it has
/// no independent identity beyond its position in the lesson.
/// </summary>
public sealed class GrammarExample
{
    // Parameterless ctor for EF Core materialization.
    private GrammarExample()
    {
        English = null!;
        Uzbek = null!;
    }

    private GrammarExample(string english, string uzbek)
    {
        English = english;
        Uzbek = uzbek;
    }

    public Guid Id { get; private set; }

    /// <summary>The exact English example sentence (target-language teaching content).</summary>
    public string English { get; private set; }

    /// <summary>A short Uzbek meaning of the sentence (learner-facing, rule 11).</summary>
    public string Uzbek { get; private set; }

    public static GrammarExample Create(string english, string? uzbek = null)
    {
        if (string.IsNullOrWhiteSpace(english))
            throw new DomainException("Grammar example English must not be empty.");
        return new GrammarExample(
            english.Trim(),
            string.IsNullOrWhiteSpace(uzbek) ? string.Empty : uzbek.Trim());
    }
}
