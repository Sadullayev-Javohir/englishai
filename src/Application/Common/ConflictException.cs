namespace Application.Common;

/// <summary>
/// Thrown when a request conflicts with the current state of a resource - e.g. claiming a
/// username another account already holds. Mapped to a 409 response by the Web error middleware
/// so the SPA can distinguish "taken" from a generic validation failure.
/// </summary>
public sealed class ConflictException : Exception
{
    public ConflictException(string message) : base(message)
    {
    }
}
