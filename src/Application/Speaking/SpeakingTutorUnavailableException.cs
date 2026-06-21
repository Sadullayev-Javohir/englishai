namespace Application.Speaking;

public sealed class SpeakingTutorUnavailableException : Exception
{
    public SpeakingTutorUnavailableException(
        string code,
        Exception? innerException = null,
        bool retryable = true)
        : base("Speaking AI is temporarily unavailable.", innerException)
    {
        Code = code;
        Retryable = retryable;
    }

    public string Code { get; }
    public bool Retryable { get; }
}
