namespace Domain.Assessment;

/// <summary>
/// The six independently measured skills in the CEFR placement test. The numeric
/// order is the order in which stages are administered. Speaking can be omitted by
/// callers such as level-exit tests, but is required by the onboarding placement.
/// </summary>
public enum TestStage
{
    Vocabulary = 1,
    Grammar = 2,
    Listening = 3,
    Reading = 4,
    Writing = 5,
    Speaking = 6
}
