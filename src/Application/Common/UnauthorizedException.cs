namespace Application.Common;

/// <summary>
/// Thrown when a request cannot be authenticated - e.g. an invalid or expired Google
/// ID token. Mapped to a 401 Unauthorized response by the Web layer's error middleware.
/// </summary>
public sealed class UnauthorizedException : Exception
{
    public UnauthorizedException(string message) : base(message)
    {
    }
}
