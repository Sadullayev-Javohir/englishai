namespace Application.Video.Ports;

/// <summary>
/// Kicks off a best-effort transcript fill for a lesson <em>off the request path</em>, so that
/// opening a video returns immediately and the quota-limited caption fetch + LLM translation
/// run in the background (PROJECT-SPEC B.3, Bosqich 3; docs/development-guide.md rule 17.3). Implementations
/// must be non-blocking and idempotent: requesting a fill that is already in flight is a no-op.
/// </summary>
public interface IVideoTranscriptFiller
{
    /// <summary>Requests a background transcript fill for the lesson. Returns immediately.</summary>
    void RequestFill(Guid videoLessonId);
}
