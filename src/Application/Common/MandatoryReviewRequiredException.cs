namespace Application.Common;

public sealed class MandatoryReviewRequiredException : Exception
{
    public const string ErrorCode = "review_required";

    public MandatoryReviewRequiredException()
        : base("Complete the due vocabulary review before continuing.")
    {
    }

    public string Code => ErrorCode;
}
