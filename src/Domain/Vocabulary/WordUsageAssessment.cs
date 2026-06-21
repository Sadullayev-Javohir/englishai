namespace Domain.Vocabulary;

/// <summary>
/// The structured result of grading a written-sentence mini-test (PROJECT-SPEC B.1): a pass/fail
/// plus a reason code, never free-form Uzbek prose (docs/development-guide.md rule 11). Produced by
/// <c>Application.Vocabulary.Ports.IWordUsageAssessor</c> implementations and used by
/// <c>SubmitReviewCommandHandler</c> to compute <see cref="MiniTestType.WrittenUsage"/> results
/// server-side instead of trusting a client-supplied boolean.
/// </summary>
public sealed record WordUsageAssessment(bool Passed, WordUsageReasonCode ReasonCode)
{
    public static WordUsageAssessment Pass() => new(true, WordUsageReasonCode.Correct);

    public static WordUsageAssessment Fail(WordUsageReasonCode reason) => new(false, reason);
}
