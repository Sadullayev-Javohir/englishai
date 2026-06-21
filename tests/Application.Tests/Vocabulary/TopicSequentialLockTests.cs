using Application.Vocabulary.Dtos;
using Domain.Assessment;
using FluentAssertions;
using Xunit;

namespace Application.Tests.Vocabulary;

/// <summary>
/// Rolling topic-window gating (PROJECT-SPEC K.5): in a level's ordered topic list mastered topics
/// and the next three unmastered topics are open.
/// </summary>
public class TopicSequentialLockTests
{
    private static VocabularyTopicSummaryDto Topic(bool mastered) =>
        new(
            Guid.NewGuid(),
            "Title",
            "Sarlavha",
            "daily_life",
            CefrLevel.A2,
            IsFilled: true,
            Learned: false,
            IsStarted: mastered,
            PassedModuleCount: mastered ? 6 : 0,
            RequiredModuleCount: 6,
            IsMastered: mastered,
            IsLocked: false,
            RequiresPro: false,
            Modules: new[]
            {
                new TopicModuleScoreDto("Vocabulary", 0, false, false, null),
                new TopicModuleScoreDto("Grammar", 0, false, false, null),
                new TopicModuleScoreDto("Reading", 0, false, false, null),
                new TopicModuleScoreDto("Writing", 0, false, false, null),
                new TopicModuleScoreDto("Speaking", 0, false, false, null),
                new TopicModuleScoreDto("Listening", 0, false, false, null),
            });

    [Fact]
    public void First_three_unmastered_topics_are_unlocked()
    {
        var locked = VocabularyTopicSummaryDto.ApplySequentialLock(new[]
        {
            Topic(mastered: false),
            Topic(mastered: false),
            Topic(mastered: false),
            Topic(mastered: false),
        });

        locked.Take(3).Should().OnlyContain(topic => !topic.IsLocked);
        locked[3].IsLocked.Should().BeTrue();
        locked.Take(3).SelectMany(topic => topic.Modules).Should().OnlyContain(module => module.Unlocked);
    }

    [Fact]
    public void Mastering_a_topic_advances_the_open_window_by_one()
    {
        var topics = new[]
        {
            Topic(mastered: true),
            Topic(mastered: false),
            Topic(mastered: false),
            Topic(mastered: false),
            Topic(mastered: false),
        };

        var locked = VocabularyTopicSummaryDto.ApplySequentialLock(topics);

        locked.Take(4).Should().OnlyContain(topic => !topic.IsLocked);
        locked[4].IsLocked.Should().BeTrue();
        locked[4].Modules.Should().OnlyContain(module => !module.Unlocked);
    }

    [Fact]
    public void All_topics_unlock_when_fewer_than_three_unmastered_topics_remain()
    {
        var topics = new[] { Topic(mastered: true), Topic(mastered: true), Topic(mastered: false) };

        var locked = VocabularyTopicSummaryDto.ApplySequentialLock(topics);

        locked.Should().OnlyContain(t => t.IsLocked == false);
    }
}
