namespace Application.Video.Ports;

/// <summary>
/// Resolves vetted Uzbek content for the video module from the content store
/// (docs/development-guide.md rule 11) - never free-generated at runtime. Currently maps quiz hint
/// codes to their Uzbek text (Video Lesson Quiz screen).
/// </summary>
public interface IVideoContentProvider
{
    /// <summary>
    /// The Uzbek hint for a question's hint code, or <c>null</c> if the code is unknown
    /// or not set.
    /// </summary>
    string? GetHint(string? hintCode);
}
