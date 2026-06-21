using Application.Common;
using Application.Vocabulary.Dtos;
using Application.Vocabulary.Ports;
using Domain.Vocabulary;
using FluentValidation;
using FluentValidation.Results;
using MediatR;

namespace Application.Vocabulary.SubmitReview;

public sealed class SubmitReviewCommandHandler : IRequestHandler<SubmitReviewCommand, ReviewResultDto>
{
    private readonly IVocabularyRepository _vocabulary;
    private readonly IWordUsageAssessor _wordUsageAssessor;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;

    public SubmitReviewCommandHandler(
        IVocabularyRepository vocabulary,
        IWordUsageAssessor wordUsageAssessor,
        TimeProvider clock,
        ICurrentUserAccessor? currentUser = null)
    {
        _vocabulary = vocabulary;
        _wordUsageAssessor = wordUsageAssessor;
        _clock = clock;
        _currentUser = currentUser;
    }

    public async Task<ReviewResultDto> Handle(SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var item = await _vocabulary.GetByIdAsync(request.VocabularyItemId, cancellationToken)
            ?? throw new NotFoundException(nameof(VocabularyItem), request.VocabularyItemId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, item.LearnerId);

        var (passed, reasonCode) = await ResolvePassedAsync(item, request, cancellationToken);

        item.RecordReview(passed, _clock.GetUtcNow());

        await _vocabulary.SaveAsync(item, cancellationToken);

        return ReviewResultDto.FromDomain(item, passed, reasonCode);
    }

    /// <summary>
    /// Computes whether the review passed (plus a reason code for a failed WrittenUsage grading).
    /// The mini-test type is re-derived from the item itself (never trusted from the client) so a
    /// learner cannot claim an easier mode to dodge verification. Cloze/written modes are checked
    /// server-side; the rest fall back to the learner's own self-rating (see
    /// <see cref="SubmitReviewCommand"/> remarks).
    /// </summary>
    private async Task<(bool Passed, WordUsageReasonCode? ReasonCode)> ResolvePassedAsync(
        VocabularyItem item, SubmitReviewCommand request, CancellationToken cancellationToken)
    {
        var miniTestType = item.NextMiniTestType();

        switch (miniTestType)
        {
            case MiniTestType.ClozeChoice:
            {
                var answer = RequireSubmittedAnswer(request);
                var passed = string.Equals(answer, item.Word.Trim(), StringComparison.OrdinalIgnoreCase);
                return (passed, null);
            }

            case MiniTestType.WrittenUsage:
            {
                var answer = RequireSubmittedAnswer(request);
                var assessment = await _wordUsageAssessor.AssessAsync(
                    item.Word, item.Translation, answer, cancellationToken);
                return (assessment.Passed, assessment.Passed ? null : assessment.ReasonCode);
            }

            case MiniTestType.SpokenUsage:
            case MiniTestType.ListeningRecognition:
            default:
                return (RequireSelfRatedPassed(request), null);
        }
    }

    private static string RequireSubmittedAnswer(SubmitReviewCommand request)
    {
        if (string.IsNullOrWhiteSpace(request.SubmittedAnswer))
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.SubmittedAnswer), "An answer is required for this review type."),
            });

        return request.SubmittedAnswer.Trim();
    }

    private static bool RequireSelfRatedPassed(SubmitReviewCommand request)
    {
        if (request.SelfRatedPassed is not { } selfRated)
            throw new ValidationException(new[]
            {
                new ValidationFailure(
                    nameof(request.SelfRatedPassed), "A self-rated result is required for this review type."),
            });

        return selfRated;
    }
}
