namespace Domain.Assessment;

/// <summary>
/// Common European Framework of Reference for Languages proficiency levels,
/// ordered from lowest (A1) to highest (C2). The numeric values are meaningful
/// and are relied upon for stepping difficulty up and down during the adaptive
/// placement test.
/// </summary>
public enum CefrLevel
{
    A1 = 1,
    A2 = 2,
    B1 = 3,
    B2 = 4,
    C1 = 5,
    C2 = 6
}
