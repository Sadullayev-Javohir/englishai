namespace Application.Assessment;

public sealed class PlacementSessionExpiredException(Guid sessionId) : Exception(
    $"Placement session '{sessionId}' expired or could not be recovered.")
{
    public const string ErrorCode = "placement_session_expired";
}
