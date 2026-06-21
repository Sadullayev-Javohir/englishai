using Domain.Learning;

namespace Domain.Writing;

/// <summary>
/// One issue the assessor found in a writing submission (PROJECT-SPEC G.3). It carries a
/// structured <see cref="IssueCode"/> (resolved to a vetted Uzbek explanation via the content
/// layer, rule 11) and the text span it refers to. For grammar issues, <see cref="Category"/>
/// maps the issue to an error-heatmap bucket so writing mistakes feed the same heatmap as the
/// rest of the platform (G.3 ↔ C.7). It is <c>null</c> for non-grammar dimensions.
/// </summary>
public sealed record WritingIssue(
    WritingDimension Dimension,
    string IssueCode,
    int StartOffset,
    int EndOffset,
    ErrorCategory? Category);
