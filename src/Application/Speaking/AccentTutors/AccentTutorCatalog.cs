namespace Application.Speaking.AccentTutors;

/// <summary>The accent tutors the product exposes. Single source of truth for every validator.</summary>
public static class AccentTutorCatalog
{
    public static readonly string[] KnownTutors = ["american", "british", "australian", "irish"];

    public static bool IsKnown(string? tutorId) =>
        tutorId is not null && KnownTutors.Contains(tutorId, StringComparer.OrdinalIgnoreCase);
}
