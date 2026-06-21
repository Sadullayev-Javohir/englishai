using Application.Assessment.Ports;
using Application.Common;
using Application.Speaking.Ports;
using MediatR;

namespace Application.Assessment.GetPlacementAudio;

public sealed class GetPlacementAudioQueryHandler
    : IRequestHandler<GetPlacementAudioQuery, PlacementAudioResult>
{
    private readonly IPlacementQuestionRepository _questions;
    private readonly ITextToSpeechService _tts;
    private readonly IPlacementAudioCache _cache;

    public GetPlacementAudioQueryHandler(
        IPlacementQuestionRepository questions,
        ITextToSpeechService tts,
        IPlacementAudioCache cache)
    {
        _questions = questions;
        _tts = tts;
        _cache = cache;
    }

    public async Task<PlacementAudioResult> Handle(
        GetPlacementAudioQuery request,
        CancellationToken cancellationToken)
    {
        var question = await _questions.GetByIdAsync(request.QuestionId, cancellationToken)
            ?? throw new NotFoundException(nameof(Domain.Assessment.PlacementQuestion), request.QuestionId);

        if (!question.HasAudio)
            throw new NotFoundException(nameof(Domain.Assessment.PlacementQuestion), request.QuestionId);

        var cached = await _cache.GetAsync(question.Id, cancellationToken);
        if (cached is null)
        {
            // Synthesize once and cache: the clip is the same for every learner.
            var speech = await _tts.SynthesizeAsync(question.AudioScript!, cancellationToken);
            if (!speech.IsNaturalVoice || speech.AudioContent.Length == 0)
                throw new PlacementAudioUnavailableException();
            var contentType = DetectContentType(speech.AudioContent);
            await _cache.SetAsync(question.Id, speech.AudioContent, contentType, cancellationToken);
            cached = await _cache.GetAsync(question.Id, cancellationToken)
                     ?? new PlacementAudioContent(speech.AudioContent, contentType, DateTimeOffset.UtcNow,
                         SizeBytes: speech.AudioContent.LongLength);
        }

        return new PlacementAudioResult(cached.Audio, cached.ContentType, cached.CreatedAt,
            cached.PublicUrl, cached.ETag, cached.SizeBytes);
    }

    /// <summary>
    /// Sniffs the audio container so the browser is told the right MIME type,
    /// regardless of whether Azure returns WAV or MP3 (or a dev stand-in returns
    /// something else).
    /// </summary>
    private static string DetectContentType(byte[] audio)
    {
        if (audio.Length >= 4 && audio[0] == 'R' && audio[1] == 'I' && audio[2] == 'F' && audio[3] == 'F')
            return "audio/wav";
        if (audio.Length >= 3 && audio[0] == 'I' && audio[1] == 'D' && audio[2] == '3')
            return "audio/mpeg";
        if (audio.Length >= 2 && audio[0] == 0xFF && (audio[1] & 0xE0) == 0xE0)
            return "audio/mpeg";
        if (audio.Length >= 4 && audio[0] == 'O' && audio[1] == 'g' && audio[2] == 'g' && audio[3] == 'S')
            return "audio/ogg";
        return "application/octet-stream";
    }
}
