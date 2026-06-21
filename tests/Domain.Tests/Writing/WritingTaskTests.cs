using Domain.Assessment;
using Domain.Common;
using Domain.Writing;
using FluentAssertions;
using Xunit;

namespace Domain.Tests.Writing;

/// <summary>
/// Covers the topic-scoped lifecycle of <see cref="WritingTask"/>: a pending shell bound to a
/// learning-spine topic that becomes filled once its prompt + guidance are generated and cached.
/// </summary>
public class WritingTaskTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);

    [Fact]
    public void ForTopic_starts_pending_with_the_word_range_but_no_prompt()
    {
        var topicId = Guid.NewGuid();

        var task = WritingTask.ForTopic(topicId, CefrLevel.A2, 50, 80, Now);

        task.VocabularyTopicId.Should().Be(topicId);
        task.Status.Should().Be(WritingTaskStatus.Pending);
        task.IsFilled.Should().BeFalse();
        task.MinWords.Should().Be(50);
        task.MaxWords.Should().Be(80);
        task.Prompt.Should().BeEmpty();
        task.Guidance.Should().BeEmpty();
    }

    [Fact]
    public void FillContent_stores_the_prompt_and_guidance_and_marks_filled()
    {
        var task = WritingTask.ForTopic(Guid.NewGuid(), CefrLevel.B1, 120, 200, Now);

        task.FillContent("Write an opinion essay about your city.", new[] { "State your opinion.", "  ", "Give reasons." });

        task.IsFilled.Should().BeTrue();
        task.Prompt.Should().Be("Write an opinion essay about your city.");
        // Blank hints are dropped.
        task.Guidance.Should().Equal("State your opinion.", "Give reasons.");
    }

    [Fact]
    public void FillContent_rejects_an_empty_prompt()
    {
        var task = WritingTask.ForTopic(Guid.NewGuid(), CefrLevel.A1, 40, 70, Now);

        var act = () => task.FillContent("   ", Array.Empty<string>());

        act.Should().Throw<DomainException>();
    }

    [Fact]
    public void ForTopic_rejects_an_empty_topic_id()
    {
        var act = () => WritingTask.ForTopic(Guid.Empty, CefrLevel.A2, 50, 80, Now);

        act.Should().Throw<DomainException>();
    }
}
