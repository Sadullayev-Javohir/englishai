using Application.Vocabulary.Dtos;
using MediatR;

namespace Application.Vocabulary.GetVocabularyTopic;

/// <summary>
/// Returns a vocabulary topic's full detail (passage + words + quiz). If the topic has not been
/// filled yet, its content is generated on demand and cached (PROJECT-SPEC module 4, rules 8/10).
/// </summary>
public sealed record GetVocabularyTopicQuery(Guid TopicId) : IRequest<VocabularyTopicDetailDto>;
