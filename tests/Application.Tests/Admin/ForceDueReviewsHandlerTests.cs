using Application.Admin.ForceDueReviews;
using Application.Common;
using Application.Identity.Dtos;
using Application.Tests.Learning;
using Application.Vocabulary.Ports;
using Application.Vocabulary.Models;
using Domain.Assessment;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Admin;

/// <summary>
/// Covers the admin "force my saved words due now" testing aid: non-admins are rejected, every
/// not-yet-due word becomes due immediately (its learning stage preserved), already-due words are
/// left untouched, and a mastered word is revived so the whole list is reviewable.
/// </summary>
public class ForceDueReviewsHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 7, 2, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Admin = Guid.NewGuid();

    private readonly IAdminAuthorization _admin = Substitute.For<IAdminAuthorization>();
    private readonly IVocabularyRepository _vocabulary = Substitute.For<IVocabularyRepository>();
    private readonly IVocabularyTopicRepository _topics = Substitute.For<IVocabularyTopicRepository>();
    private readonly IVocabularyPassageGenerator _generator = Substitute.For<IVocabularyPassageGenerator>();
    private readonly ForceDueReviewsCommandHandler _handler;

    public ForceDueReviewsHandlerTests()
    {
        _admin.GetRoleAsync(Admin, Arg.Any<CancellationToken>()).Returns(AdminRole.Admin);
        _handler = new ForceDueReviewsCommandHandler(
            _admin, _vocabulary, _topics, _generator, new FixedTimeProvider(Now));
    }

    [Fact]
    public async Task Non_admin_is_forbidden()
    {
        var learner = Guid.NewGuid();
        _admin.GetRoleAsync(learner, Arg.Any<CancellationToken>()).Returns(AdminRole.None);

        var act = () => _handler.Handle(new ForceDueReviewsCommand(learner), CancellationToken.None);

        await act.Should().ThrowAsync<ForbiddenException>();
    }

    [Fact]
    public async Task Makes_every_saved_word_due_now_without_touching_already_due_ones()
    {
        // Saved just now - its first review would normally land 3 days out (not due).
        var fresh = VocabularyItem.Learn(Admin, "resilient", "chidamli", Now);
        // Saved 4 days ago - already past its day-3 checkpoint, so it is due as-is.
        var alreadyDue = VocabularyItem.Learn(Admin, "keen", "ishtiyoqli", Now.AddDays(-4));
        _vocabulary.GetByLearnerIdAsync(Admin, Arg.Any<CancellationToken>())
            .Returns(new[] { fresh, alreadyDue });

        var result = await _handler.Handle(new ForceDueReviewsCommand(Admin), CancellationToken.None);

        result.TotalWords.Should().Be(2);
        result.MadeDue.Should().Be(1); // only the fresh word needed forcing
        result.DueNow.Should().Be(2); // both wait in the review queue now

        fresh.IsDue(Now).Should().BeTrue();
        fresh.Schedule.Stage.Should().Be(ReviewStage.Day3); // stage untouched - no fabricated progress
        await _vocabulary.Received(1).SaveAsync(fresh, Arg.Any<CancellationToken>());
        await _vocabulary.DidNotReceive().SaveAsync(alreadyDue, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Revives_a_mastered_word_back_to_the_first_checkpoint()
    {
        var mastered = VocabularyItem.Learn(Admin, "thrive", "gullab-yashnamoq", Now.AddDays(-60));
        mastered.RecordReview(passed: true, Now.AddDays(-57)); // Day3 → Day7
        mastered.RecordReview(passed: true, Now.AddDays(-50)); // Day7 → Day21
        mastered.RecordReview(passed: true, Now.AddDays(-20)); // Day21 → Mastered
        mastered.Schedule.Stage.Should().Be(ReviewStage.Mastered);
        _vocabulary.GetByLearnerIdAsync(Admin, Arg.Any<CancellationToken>())
            .Returns(new[] { mastered });

        var result = await _handler.Handle(new ForceDueReviewsCommand(Admin), CancellationToken.None);

        result.MadeDue.Should().Be(1);
        result.DueNow.Should().Be(1);
        mastered.Schedule.Stage.Should().Be(ReviewStage.Day3);
        mastered.IsDue(Now).Should().BeTrue();
    }

    [Fact]
    public async Task Seeds_a1_my_family_words_when_admin_has_no_saved_vocabulary()
    {
        var topic = VocabularyTopic.Curate(
            "a1-my-family", "My Family", "Mening oilam", "family_people", "to-be", CefrLevel.A1, Now);
        topic.FillContent(
            "My family is small.",
            new[]
            {
                TopicWord.Create("mother", "ona", "My mother is kind.", PartOfSpeech.Noun),
                TopicWord.Create("father", "ota", "My father is tall.", PartOfSpeech.Noun),
            });
        _vocabulary.GetByLearnerIdAsync(Admin, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyItem>());
        _topics.GetByLevelAsync(CefrLevel.A1, Arg.Any<CancellationToken>())
            .Returns(new[] { topic });

        var result = await _handler.Handle(new ForceDueReviewsCommand(Admin), CancellationToken.None);

        result.TotalWords.Should().Be(2);
        result.SeededWords.Should().Be(2);
        result.DueNow.Should().Be(2);
        await _vocabulary.Received(2).SaveAsync(Arg.Any<VocabularyItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Generates_my_family_content_before_seeding_when_topic_is_pending()
    {
        var topic = VocabularyTopic.Curate(
            "a1-my-family", "My Family", "Mening oilam", "family_people", "to-be", CefrLevel.A1, Now);
        _vocabulary.GetByLearnerIdAsync(Admin, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyItem>());
        _topics.GetByLevelAsync(CefrLevel.A1, Arg.Any<CancellationToken>())
            .Returns(new[] { topic });
        _generator.GenerateAsync("My Family", CefrLevel.A1, VocabularyTopic.TargetWordCount, Arg.Any<CancellationToken>())
            .Returns(new GeneratedTopicContent(
                "My sister is young.",
                new[] { new GeneratedTopicWord("sister", "opa-singil", "My sister is young.", "noun") }));

        var result = await _handler.Handle(new ForceDueReviewsCommand(Admin), CancellationToken.None);

        result.SeededWords.Should().Be(1);
        result.DueNow.Should().Be(1);
        await _topics.Received(1).SaveAsync(topic, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Creates_my_family_topic_when_catalog_entry_is_missing()
    {
        _vocabulary.GetByLearnerIdAsync(Admin, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyItem>());
        _topics.GetByLevelAsync(CefrLevel.A1, Arg.Any<CancellationToken>())
            .Returns(Array.Empty<VocabularyTopic>());
        _generator.GenerateAsync("My Family", CefrLevel.A1, VocabularyTopic.TargetWordCount, Arg.Any<CancellationToken>())
            .Returns(new GeneratedTopicContent(
                "My brother is young.",
                new[] { new GeneratedTopicWord("brother", "aka-uka", "My brother is young.", "noun") }));

        var result = await _handler.Handle(new ForceDueReviewsCommand(Admin), CancellationToken.None);

        result.TotalWords.Should().Be(1);
        result.SeededWords.Should().Be(1);
        result.DueNow.Should().Be(1);
        await _topics.Received(1).SaveAsync(
            Arg.Is<VocabularyTopic>(topic => topic.Slug == "a1-my-family" && topic.IsFilled),
            Arg.Any<CancellationToken>());
    }
}
