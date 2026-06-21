using Application.Speaking.Dtos;
using Application.Speaking.Ports;
using Application.Vocabulary.Dtos;
using Domain.Speaking;
using MediatR;

namespace Application.Vocabulary.CheckWordPronunciation;

public sealed class CheckWordPronunciationCommandHandler
    : IRequestHandler<CheckWordPronunciationCommand, WordPronunciationCheckDto>
{
    private readonly IPronunciationAssessor _assessor;
    private readonly IFeedbackTemplateProvider _feedback;

    public CheckWordPronunciationCommandHandler(
        IPronunciationAssessor assessor, IFeedbackTemplateProvider feedback)
    {
        _assessor = assessor;
        _feedback = feedback;
    }

    public async Task<WordPronunciationCheckDto> Handle(
        CheckWordPronunciationCommand request, CancellationToken cancellationToken)
    {
        var word = request.Word.Trim();

        // No audio captured: ask the learner to try again (don't fabricate a score).
        if (request.AudioContent.Length == 0)
            return new WordPronunciationCheckDto(
                word, Recognized: false, Correct: false, IsAuthentic: false, 0, 0,
                PronunciationErrorType.None, Array.Empty<PhonemePronunciationDto>(),
                _feedback.Get("pron.not_recognized"));

        // Assess the clip against the known target word (the reference text).
        var result = await _assessor.AssessAsync(request.AudioContent, word, cancellationToken);

        var recognized = result.IsAuthentic && result.OverallScore > 0 && result.Words.Count > 0;
        if (!recognized)
            return new WordPronunciationCheckDto(
                word, Recognized: false, Correct: false, IsAuthentic: false, 0, 0,
                PronunciationErrorType.None, Array.Empty<PhonemePronunciationDto>(),
                _feedback.Get("pron.not_recognized"));

        // The word's own breakdown (the reference is a single word, so take the matching/first one).
        var focus = result.Words.FirstOrDefault(
                        w => string.Equals(w.Word, word, StringComparison.OrdinalIgnoreCase))
                    ?? result.Words[0];

        var correct = !focus.NeedsPractice && result.OverallScore >= SpeakingScoreThresholds.GoodOverall;

        var feedbackUz = _feedback.Get(
            FeedbackCode(correct, focus.ErrorType),
            new Dictionary<string, string> { ["word"] = word });

        return new WordPronunciationCheckDto(
            word,
            Recognized: true,
            correct,
            IsAuthentic: true,
            Math.Round(result.OverallScore, 1),
            Math.Round(focus.AccuracyScore, 1),
            focus.ErrorType,
            focus.Phonemes.Select(PhonemePronunciationDto.FromDomain).ToList(),
            feedbackUz);
    }

    private static string FeedbackCode(bool correct, PronunciationErrorType errorType)
    {
        if (correct)
            return "pron.good";

        return errorType switch
        {
            PronunciationErrorType.Mispronunciation => "pron.mispronunciation",
            PronunciationErrorType.Omission => "pron.omission",
            PronunciationErrorType.Insertion => "pron.insertion",
            _ => "pron.low_accuracy"
        };
    }
}
