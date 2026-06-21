namespace Application.Common;

/// <summary>
/// Thrown when a learner tries to open a CEFR level, topic, or skill that has not been unlocked by
/// their placement level and topic progress. Mapped to HTTP 423 so clients can distinguish a
/// progression gate from authentication and subscription failures.
/// </summary>
public sealed class TopicLockedException : Exception
{
    public const string ErrorCode = "topic_locked";

    public TopicLockedException() : base("Complete the preceding learning steps to unlock this topic.")
    {
    }

    public string Code => ErrorCode;
}
