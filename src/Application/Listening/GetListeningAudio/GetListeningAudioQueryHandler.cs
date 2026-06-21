using Application.Common;
using Application.Listening.Dtos;
using Application.Listening.Ports;
using Application.Speaking.Ports;
using MediatR;

namespace Application.Listening.GetListeningAudio;

public sealed class GetListeningAudioQueryHandler
    : IRequestHandler<GetListeningAudioQuery, ListeningAudioResult>
{
    private readonly IListeningRepository _exercises;
    private readonly ITextToSpeechService _tts;
    private readonly IListeningAudioCache _cache;

    public GetListeningAudioQueryHandler(
        IListeningRepository exercises,
        ITextToSpeechService tts,
        IListeningAudioCache cache)
    {
        _exercises = exercises;
        _tts = tts;
        _cache = cache;
    }

    public async Task<ListeningAudioResult> Handle(
        GetListeningAudioQuery request, CancellationToken cancellationToken)
    {
        // The player loads the exercise (which generates + caches it) before the audio element
        // requests the clip, so the exercise is already filled by the time we reach here.
        var exercise = await _exercises.GetByTopicIdAsync(request.TopicId, cancellationToken)
                       ?? throw new NotFoundException("Listening exercise", request.TopicId);

        var cached = await _cache.GetAsync(exercise.Id, cancellationToken);
        if (cached is null)
        {
            // Synthesize once and cache: the clip is identical for every learner (rule 10).
            var speech = await _tts.SynthesizeAsync(exercise.Transcript, cancellationToken);
            var contentType = DetectContentType(speech.AudioContent);
            await _cache.SetAsync(exercise.Id, speech.AudioContent, contentType, cancellationToken);
            cached = await _cache.GetAsync(exercise.Id, cancellationToken)
                     ?? new ListeningAudioContent(speech.AudioContent, contentType, DateTimeOffset.UtcNow,
                         SizeBytes: speech.AudioContent.LongLength);
        }

        return new ListeningAudioResult(cached.Audio, cached.ContentType, cached.CreatedAt,
            cached.PublicUrl, cached.ETag, cached.SizeBytes);
    }

    /// <summary>
    /// Sniffs the audio container so the browser is told the right MIME type, regardless of
    /// whether Azure returns WAV or MP3 (or a dev stand-in returns something else).
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
