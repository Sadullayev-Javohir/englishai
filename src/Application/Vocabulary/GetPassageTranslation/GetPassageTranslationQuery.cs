using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.GetPassageTranslation;

/// <summary>
/// Returns the topic's passage split into sentences, each paired with its Uzbek translation, in
/// reading order. The client reveals them one pair at a time on the "MATN" card. Translation is
/// cache-first (rules 10/11) and any sentence that fails to translate keeps a null <see
/// cref="PassageSentenceDto.Uzbek"/> rather than fabricated text (rules 8/11).
/// </summary>
public sealed record GetPassageTranslationQuery(Guid TopicId)
    : IRequest<VocabularyTopicPassageTranslationDto>;
