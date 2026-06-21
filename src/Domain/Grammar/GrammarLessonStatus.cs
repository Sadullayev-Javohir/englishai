namespace Domain.Grammar;

/// <summary>
/// Lifecycle of a topic-scoped grammar lesson. A lesson bound to a learning-spine topic starts
/// <see cref="Pending"/> (a shell created from the topic's grammar focus) and becomes
/// <see cref="Filled"/> once the LLM has generated its five steps and they are cached (rules 8, 10).
/// Legacy curated lessons are created already <see cref="Filled"/>.
/// </summary>
public enum GrammarLessonStatus
{
    Pending = 0,
    Filled = 1
}
