using Domain.Assessment;
using Domain.Speaking;

namespace Application.Speaking.Dtos;

/// <summary>
/// One free-talk conversation topic the learner can pick under "Erkin suhbat", for the topic
/// picker. The Uzbek label is looked up in the frontend content store by <see cref="Code"/>
/// (docs/development-guide.md rule 11); <see cref="EnglishTitle"/> is only a reference/fallback. The client sends the
/// <see cref="Code"/> back as the conversation topic when the learner starts a chat about it.
/// <see cref="ImageId"/> keys the topic's licensed illustration in the shared topic-image store
/// (served from /api/images/topics/{imageId} - rule 12), so every card shows a relevant photo.
/// </summary>
public sealed record FreeTalkTopicDto(string Code, string EnglishTitle, CefrLevel Level, Guid ImageId)
{
    public static FreeTalkTopicDto FromDomain(FreeTalkTopic topic) =>
        new(topic.Code, topic.EnglishTitle, topic.Level, topic.ImageId);
}
