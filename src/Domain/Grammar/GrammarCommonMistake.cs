using Domain.Common;

namespace Domain.Grammar;

/// <summary>
/// A common mistake Uzbek learners make with a grammar focus, written in Uzbek (learner-facing,
/// docs/development-guide.md rule 11). Owned by <see cref="GrammarLesson"/> (EF Core <c>OwnsMany</c>) so it has no
/// independent identity beyond its position in the lesson.
/// </summary>
public sealed class GrammarCommonMistake
{
    // Parameterless ctor for EF Core materialization.
    private GrammarCommonMistake()
    {
        Text = null!;
    }

    private GrammarCommonMistake(string text)
    {
        Text = text;
    }

    public Guid Id { get; private set; }

    /// <summary>The Uzbek description of the common mistake.</summary>
    public string Text { get; private set; }

    public static GrammarCommonMistake Create(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            throw new DomainException("A grammar common mistake must not be empty.");
        return new GrammarCommonMistake(text.Trim());
    }
}
