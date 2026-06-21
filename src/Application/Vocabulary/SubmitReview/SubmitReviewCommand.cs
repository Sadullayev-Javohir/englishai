using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.SubmitReview;

/// <summary>
/// Records the outcome of a review mini-test for a word (PROJECT-SPEC B.1). A pass advances the
/// word along the 3/7/21 ladder; a failure restarts its schedule.
///
/// The mini-test type is never taken from the client - the handler re-derives it from the item's
/// own <c>NextMiniTestType()</c> (schedule stage + source), the same value <c>GetDueReviewsQuery</c>
/// showed the learner. For the modes the server can objectively check
/// (<see cref="Domain.Vocabulary.MiniTestType.ClozeChoice"/>,
/// <see cref="Domain.Vocabulary.MiniTestType.WrittenUsage"/>) it verifies
/// <see cref="SubmittedAnswer"/> itself rather than trusting a client-asserted pass/fail. For the
/// modes it cannot yet check server-side (<see cref="Domain.Vocabulary.MiniTestType.SpokenUsage"/>,
/// <see cref="Domain.Vocabulary.MiniTestType.ListeningRecognition"/> - both need Azure Speech/audio
/// infrastructure not wired up yet) it falls back to <see cref="SelfRatedPassed"/>, the learner's
/// own flip-card self-assessment - a legitimate SRS mechanic on its own, not a workaround.
/// </summary>
public sealed record SubmitReviewCommand(
    Guid VocabularyItemId,
    string? SubmittedAnswer = null,
    bool? SelfRatedPassed = null) : IRequest<ReviewResultDto>;
