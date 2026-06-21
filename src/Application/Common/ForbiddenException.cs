namespace Application.Common;

/// <summary>
/// Thrown when an authenticated caller tries to act on data that is not theirs. Mapped to
/// a 403 Forbidden response by the Web layer's error middleware (distinct from 401, which
/// means "not authenticated at all").
/// </summary>
public sealed class ForbiddenException : Exception
{
    public ForbiddenException(string message) : base(message)
    {
    }
}
