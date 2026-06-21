using Application.Tests.Common;
using Application.Vocabulary;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Vocabulary;

public class TopicVocabularyEnrollmentServiceTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 30, 8, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task Enroll_saves_every_topic_word_for_day_three_review()
    {
        var topic = FilledTopic();
        var learnerId = Guid.NewGuid();
        var repository = EmptyVocabulary();
        var service = new TopicVocabularyEnrollmentService(repository);

        var result = await service.EnrollAsync(learnerId, topic, Now, CancellationToken.None);

        result.InsertedCount.Should().Be(topic.Words.Count);
        await repository.Received(topic.Words.Count).SaveAsync(
            Arg.Is<VocabularyItem>(item =>
                item.LearnerId == learnerId &&
                item.SourceTopicId == topic.Id &&
                item.Schedule.NextReviewAt == Now.AddDays(ReviewSchedule.Day3IntervalDays)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Enroll_skips_words_already_saved_from_the_same_topic()
    {
        var topic = FilledTopic();
        var learnerId = Guid.NewGuid();
        var existing = VocabularyItem.Learn(
            learnerId, topic.Words[0].Word, topic.Words[0].Translation, Now.AddDays(-5), sourceTopicId: topic.Id);
        var repository = Substitute.For<IVocabularyRepository>();
        repository.GetByLearnerIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns(new[] { existing });
        var service = new TopicVocabularyEnrollmentService(repository);

        var result = await service.EnrollAsync(learnerId, topic, Now, CancellationToken.None);

        result.ExistingCount.Should().Be(1);
        result.InsertedCount.Should().Be(topic.Words.Count - 1);
        await repository.DidNotReceive().SaveAsync(
            Arg.Is<VocabularyItem>(item => item.Word == existing.Word),
            Arg.Any<CancellationToken>());
    }

    private static VocabularyTopic FilledTopic()
    {
        var topic = VocabularyTopic.Curate(
            "a1-daily-life", "Daily Life", "Kundalik hayot", "life", "present-simple", CefrLevel.A1, Now);
        topic.FillContent("Daily life.", new[]
        {
            TopicWord.Create("wake", "uyg'onmoq", "I wake up early.", PartOfSpeech.Verb),
            TopicWord.Create("breakfast", "nonushta", "Breakfast is ready.", PartOfSpeech.Noun),
            TopicWord.Create("early", "erta", "I arrive early.", PartOfSpeech.Adverb),
        });
        return topic;
    }

    private static IVocabularyRepository EmptyVocabulary()
    {
        var repository = Substitute.For<IVocabularyRepository>();
        repository.GetByLearnerIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyItem>());
        return repository;
    }
}
