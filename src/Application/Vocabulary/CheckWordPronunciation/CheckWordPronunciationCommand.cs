using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.CheckWordPronunciation;

/// <summary>
/// Checks whether the learner pronounced a single target word correctly (PROJECT-SPEC module 4 /
/// B.1 "Og'zaki ishlatish"): the audio is assessed against the known target word and the result
/// reports correct/incorrect with a vetted Uzbek feedback line. <see cref="AudioContent"/> is the
/// raw clip the API binds from a base64 body field.
/// </summary>
public sealed record CheckWordPronunciationCommand(string Word, byte[] AudioContent)
    : IRequest<WordPronunciationCheckDto>;
