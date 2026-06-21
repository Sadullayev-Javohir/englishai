using Application.Common;
using Application.Speaking.Dtos;
using Application.Speaking.Ports;
using Application.Subscription.Entitlements;
using Application.Vocabulary.Dtos;
using Domain.Speaking;
using Domain.Subscription;
using MediatR;

namespace Application.Speaking.PracticeWords;

public sealed class SubmitSpeakingPracticeAttemptCommandHandler
    : IRequestHandler<SubmitSpeakingPracticeAttemptCommand, SpeakingPracticeAttemptResult>
{
    private readonly ISpeakingPracticeWordRepository _repository;
    private readonly IPronunciationAssessor _assessor;
    private readonly IFeedbackTemplateProvider _feedback;
    private readonly TimeProvider _clock;
    private readonly ICurrentUserAccessor? _currentUser;
    private readonly IEntitlementService? _entitlements;

    public SubmitSpeakingPracticeAttemptCommandHandler(
        ISpeakingPracticeWordRepository repository,
        IPronunciationAssessor assessor,
        IFeedbackTemplateProvider feedback,
        TimeProvider clock,
        ICurrentUserAccessor? currentUser = null,
        IEntitlementService? entitlements = null)
    {
        _repository = repository;
        _assessor = assessor;
        _feedback = feedback;
        _clock = clock;
        _currentUser = currentUser;
        _entitlements = entitlements;
    }

    public async Task<SpeakingPracticeAttemptResult> Handle(
        SubmitSpeakingPracticeAttemptCommand request,
        CancellationToken cancellationToken)
    {
        var practiceWord = await _repository.GetByIdAsync(request.PracticeWordId, cancellationToken)
            ?? throw new NotFoundException(nameof(SpeakingPracticeWord), request.PracticeWordId);
        ResourceOwnership.EnsureCurrentLearner(_currentUser, practiceWord.LearnerId);

        // Azure Pronunciation Assessment is billed per audio hour on top of speech-to-text, so the
        // dedicated drills carry their own daily budget (docs/development-guide.md rule 10).
        if (_entitlements is not null)
        {
            await _entitlements.EnsureAllowedAsync(
                practiceWord.LearnerId, PremiumFeature.PronunciationDrill, cancellationToken);
        }

        var assessment = await AssessAsync(practiceWord.Word, request.AudioContent, cancellationToken);
        if (_entitlements is not null)
        {
            await _entitlements.RecordUsageAsync(
                practiceWord.LearnerId, PremiumFeature.PronunciationDrill, cancellationToken);
        }

        if (assessment.Recognized && assessment.IsAuthentic)
        {
            practiceWord.RecordPractice(assessment.OverallScore, _clock.GetUtcNow());
            await _repository.SaveAsync(practiceWord, cancellationToken);
        }

        // The word is mastered only after the learner clears the score the required number
        // of times; a single success just advances SuccessfulAttemptCount.
        return new SpeakingPracticeAttemptResult(
            assessment,
            Mastered: practiceWord.MasteredAt is not null,
            SpeakingPracticeThresholds.MasteryScore,
            practiceWord.SuccessfulAttemptCount,
            SpeakingPracticeThresholds.RequiredSuccesses);
    }

    private async Task<WordPronunciationCheckDto> AssessAsync(
        string word,
        byte[] audioContent,
        CancellationToken cancellationToken)
    {
        if (audioContent.Length == 0)
            return EmptyResult(word);

        var result = await _assessor.AssessAsync(audioContent, word, cancellationToken);
        var recognized = result.IsAuthentic && result.OverallScore > 0 && result.Words.Count > 0;
        if (!recognized)
            return EmptyResult(word);

        var focus = result.Words.FirstOrDefault(candidate =>
                        string.Equals(candidate.Word, word, StringComparison.OrdinalIgnoreCase))
                    ?? result.Words[0];
        var correct = !focus.NeedsPractice && result.OverallScore >= SpeakingScoreThresholds.GoodOverall;

        return new WordPronunciationCheckDto(
            word,
            Recognized: true,
            correct,
            IsAuthentic: true,
            Math.Round(result.OverallScore, 1),
            Math.Round(focus.AccuracyScore, 1),
            focus.ErrorType,
            focus.Phonemes.Select(PhonemePronunciationDto.FromDomain).ToList(),
            _feedback.Get(
                correct ? "pron.good" : FeedbackCode(focus.ErrorType),
                new Dictionary<string, string> { ["word"] = word }));
    }

    private WordPronunciationCheckDto EmptyResult(string word) => new(
        word, false, false, false, 0, 0, PronunciationErrorType.None,
        Array.Empty<PhonemePronunciationDto>(), _feedback.Get("pron.not_recognized"));

    private static string FeedbackCode(PronunciationErrorType errorType) => errorType switch
    {
        PronunciationErrorType.Mispronunciation => "pron.mispronunciation",
        PronunciationErrorType.Omission => "pron.omission",
        PronunciationErrorType.Insertion => "pron.insertion",
        _ => "pron.low_accuracy"
    };
}
