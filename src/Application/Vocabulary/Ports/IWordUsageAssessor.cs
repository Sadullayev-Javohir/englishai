using Domain.Vocabulary;

namespace Application.Vocabulary.Ports;

/// <summary>
/// Port for grading the <see cref="MiniTestType.WrittenUsage"/> SRS mini-test (PROJECT-SPEC B.1):
/// "does this sentence correctly use the target word?". Deliberately lighter than
/// <c>Application.Writing.Ports.IWritingAssessor</c> - that port scores a multi-sentence essay
/// across four rubric dimensions, which is the wrong shape for judging one short sentence's use
/// of a single word. An implementation returns a structured pass/fail + reason code (docs/development-guide.md
/// rule 11), never free Uzbek prose.
/// </summary>
public interface IWordUsageAssessor
{
    Task<WordUsageAssessment> AssessAsync(
        string word,
        string translation,
        string submittedSentence,
        CancellationToken cancellationToken);
}
