namespace Application.Listening.Ports;

/// <summary>
/// Resolves vetted Uzbek content for the listening module from the content store
/// (docs/development-guide.md rule 11) - never free-generated at runtime. Currently maps quiz hint codes
/// to their Uzbek text (shown only for a wrong answer).
/// </summary>
public interface IListeningContentProvider
{
    /// <summary>
    /// The Uzbek hint for a question's hint code, or <c>null</c> if the code is unknown or
    /// not set.
    /// </summary>
    string? GetHint(string? hintCode);
}
