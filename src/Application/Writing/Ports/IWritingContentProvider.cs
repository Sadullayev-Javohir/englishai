namespace Application.Writing.Ports;

/// <summary>
/// Resolves vetted Uzbek content for the writing module from the content store
/// (docs/development-guide.md rule 11) - never free-generated at runtime. Maps an issue code (e.g.
/// <c>subject_verb_agreement</c>) to its Uzbek explanation shown next to the located text
/// span.
/// </summary>
public interface IWritingContentProvider
{
    /// <summary>
    /// The Uzbek explanation for an issue code, or <c>null</c> if the code is unknown or not
    /// set.
    /// </summary>
    string? GetIssueExplanation(string? issueCode);
}
