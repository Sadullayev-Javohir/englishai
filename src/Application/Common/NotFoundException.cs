namespace Application.Common;

/// <summary>
/// Thrown by handlers when a requested aggregate or entity does not exist.
/// Mapped to a 404 response by the Web layer's error middleware.
/// </summary>
public sealed class NotFoundException : Exception
{
    public NotFoundException(string entity, object key)
        : base($"{entity} with key '{key}' was not found.")
    {
    }
}
