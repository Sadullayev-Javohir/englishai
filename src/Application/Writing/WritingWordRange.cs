using Domain.Assessment;

namespace Application.Writing;

/// <summary>
/// The expected word range for a writing task at a given CEFR level (PROJECT-SPEC G.3). The genre
/// itself varies per topic (see <see cref="Content.WritingGenreCatalog"/>) - short personal texts at
/// A1–A2, opinion/transactional pieces at B1–B2, extended argument and formal genres at C1+ - but the
/// length band is fixed from the level so it is known even while the prompt is still pending.
/// </summary>
public static class WritingWordRange
{
    public static (int Min, int Max) For(CefrLevel level) => level switch
    {
        CefrLevel.A1 => (40, 70),
        CefrLevel.A2 => (50, 80),
        CefrLevel.B1 => (120, 200),
        CefrLevel.B2 => (150, 250),
        CefrLevel.C1 => (200, 300),
        _ => (250, 350),
    };

    /// <summary>
    /// Fraction by which a submission may exceed the expected range max before it is refused.
    /// A small tolerance absorbs honest overflow and the slight word-counting differences between
    /// the client and server, while still bounding the (paid) LLM assessment cost.
    /// </summary>
    private const double OverflowTolerance = 0.3;

    /// <summary>
    /// The hard upper bound on a submission's word count for a task whose expected max is
    /// <paramref name="maxWords"/>. The AI writing assessment is a paid LLM call (docs/development-guide.md rule 10),
    /// so a text far longer than the level's range is rejected before the call rather than billed.
    /// </summary>
    public static int HardCap(int maxWords) => (int)Math.Ceiling(maxWords * (1 + OverflowTolerance));
}
