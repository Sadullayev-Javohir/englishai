namespace Domain.Common;

/// <summary>
/// Base type for all domain rule violations. The Web layer maps these to
/// client-facing error responses through global error middleware.
/// </summary>
public class DomainException : Exception
{
    public DomainException(string message) : base(message)
    {
    }
}
