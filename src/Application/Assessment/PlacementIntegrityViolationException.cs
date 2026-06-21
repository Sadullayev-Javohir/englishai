namespace Application.Assessment;

public sealed class PlacementIntegrityViolationException(Guid sessionId) : Exception(
    $"Placement session '{sessionId}' was invalidated because the secure test rules were violated.")
{
    public const string ErrorCode = "placement_integrity_violation";
}
