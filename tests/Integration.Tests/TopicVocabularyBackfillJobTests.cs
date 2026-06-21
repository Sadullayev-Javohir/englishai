using Application.Vocabulary;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Learning;
using Domain.Vocabulary;
using FluentAssertions;
using Infrastructure.Backfill;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Integration.Tests;

public class TopicVocabularyBackfillJobTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Backfill_enrolls_words_for_records_with_vocabulary_progress()
    {
        var learnerId = Guid.NewGuid();
        var topic = FilledTopic();
        var record = TopicCompletionRecord.Start(learnerId, topic.Id, topic.Level, Now.AddDays(-10));
        record.RecordModule(SkillType.Vocabulary, 80, Now.AddDays(-10));
        var completions = Substitute.For<ITopicCompletionStore>();
        completions.GetWithVocabularyProgressAsync(Arg.Any<CancellationToken>()).Returns(new[] { record });
        var topics = Substitute.For<IVocabularyTopicRepository>();
        topics.GetByIdsAsync(Arg.Any<IReadOnlyCollection<Guid>>(), Arg.Any<CancellationToken>())
            .Returns(new[] { topic });
        var vocabulary = Substitute.For<IVocabularyRepository>();
        vocabulary.GetByLearnerIdAsync(learnerId, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyItem>());
        var job = new TopicVocabularyBackfillJob(
            completions,
            topics,
            new TopicVocabularyEnrollmentService(vocabulary),
            new FixedTimeProvider(Now),
            NullLogger<TopicVocabularyBackfillJob>.Instance);

        var result = await job.RunAsync(CancellationToken.None);

        result.Should().Be(new TopicVocabularyBackfillResult(1, 0, topic.Words.Count, 0));
        await vocabulary.Received(topic.Words.Count).SaveAsync(
            Arg.Is<VocabularyItem>(item => item.Schedule.NextReviewAt == Now.AddDays(3)),
            Arg.Any<CancellationToken>());
    }

    private static VocabularyTopic FilledTopic()
    {
        var topic = VocabularyTopic.Curate(
            "a1-daily-life", "Daily Life", "Kundalik hayot", "life", "present-simple", CefrLevel.A1, Now);
        topic.FillContent("Daily life.", new[]
        {
            TopicWord.Create("wake", "uyg'onmoq", "I wake up early."),
            TopicWord.Create("breakfast", "nonushta", "Breakfast is ready."),
        });
        return topic;
    }

    private sealed class FixedTimeProvider : TimeProvider
    {
        private readonly DateTimeOffset _now;

        public FixedTimeProvider(DateTimeOffset now)
        {
            _now = now;
        }

        public override DateTimeOffset GetUtcNow() => _now;
    }
}
