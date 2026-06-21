using Application.Common;
using Application.Notifications;
using Application.Tests.Learning;
using Application.Notifications.DispatchDueReviewNotifications;
using Application.Notifications.Ports;
using Application.Subscription.Entitlements;
using Application.Vocabulary.GetDueReviews;
using Application.Vocabulary.LearnWord;
using Application.Vocabulary.Ports;
using Application.Vocabulary.SubmitReview;
using Domain.Notifications;
using Domain.Vocabulary;
using FluentAssertions;
using FluentValidation;
using NSubstitute;
using Xunit;

namespace Application.Tests.Vocabulary;

public class VocabularyHandlersTests
{
    private static readonly DateTimeOffset Now = new(2026, 6, 22, 9, 0, 0, TimeSpan.Zero);
    private static readonly Guid Learner = Guid.NewGuid();

    private readonly IVocabularyRepository _vocabulary = Substitute.For<IVocabularyRepository>();
    private readonly IWordUsageAssessor _wordUsageAssessor = Substitute.For<IWordUsageAssessor>();
    private readonly IEntitlementService _entitlements = Substitute.For<IEntitlementService>();
    private readonly TimeProvider _clock = new FixedTimeProvider(Now);

    private static VocabularyItem Word(string word = "innovation") =>
        VocabularyItem.Learn(Learner, word, "yangilik", Now);

    [Fact]
    public async Task LearnWord_creates_item_scheduled_for_day3_and_persists()
    {
        var handler = new LearnWordCommandHandler(_vocabulary, _entitlements, _clock);

        var dto = await handler.Handle(
            new LearnWordCommand(Learner, "innovation", "yangilik"), CancellationToken.None);

        dto.Word.Should().Be("innovation");
        dto.Stage.Should().Be(ReviewStage.Day3);
        dto.NextReviewAt.Should().Be(Now.AddDays(3));
        await _vocabulary.Received(1).SaveAsync(Arg.Any<VocabularyItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitReview_ClozeChoice_correct_answer_passes_and_advances_stage()
    {
        // Day3 → ClozeChoice (VocabularyItem.NextMiniTestType). The correct word answer is checked
        // server-side against item.Word - never a client-supplied boolean.
        var item = Word();
        _vocabulary.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new SubmitReviewCommandHandler(_vocabulary, _wordUsageAssessor, _clock);

        var result = await handler.Handle(
            new SubmitReviewCommand(item.Id, SubmittedAnswer: " Innovation "), CancellationToken.None);

        result.Stage.Should().Be(ReviewStage.Day7);
        result.Mastered.Should().BeFalse();
        result.Passed.Should().BeTrue();
        await _vocabulary.Received(1).SaveAsync(item, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitReview_ClozeChoice_wrong_answer_fails_even_if_client_claims_self_rated_pass()
    {
        // A learner cannot dodge server verification by also sending SelfRatedPassed=true - the
        // handler derives the mode from the item and, for ClozeChoice, only trusts the answer text.
        var item = Word();
        _vocabulary.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new SubmitReviewCommandHandler(_vocabulary, _wordUsageAssessor, _clock);

        var result = await handler.Handle(
            new SubmitReviewCommand(item.Id, SubmittedAnswer: "banana", SelfRatedPassed: true),
            CancellationToken.None);

        result.Stage.Should().Be(ReviewStage.Day3);
        result.FailCount.Should().Be(1);
        result.Passed.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitReview_ClozeChoice_without_an_answer_throws_validation_error()
    {
        var item = Word();
        _vocabulary.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new SubmitReviewCommandHandler(_vocabulary, _wordUsageAssessor, _clock);

        var act = () => handler.Handle(
            new SubmitReviewCommand(item.Id, SelfRatedPassed: true), CancellationToken.None);

        await act.Should().ThrowAsync<ValidationException>();
        await _vocabulary.DidNotReceive().SaveAsync(Arg.Any<VocabularyItem>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SubmitReview_WrittenUsage_asks_the_word_usage_assessor_and_trusts_its_verdict()
    {
        // Day7 → WrittenUsage. The pass/fail comes from IWordUsageAssessor's structured result, not
        // from the client.
        var item = Word();
        item.RecordReview(true, Now); // Day3 → Day7
        _vocabulary.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _wordUsageAssessor
            .AssessAsync(item.Word, item.Translation, "The innovation changed everything.", Arg.Any<CancellationToken>())
            .Returns(WordUsageAssessment.Pass());
        var handler = new SubmitReviewCommandHandler(_vocabulary, _wordUsageAssessor, _clock);

        var result = await handler.Handle(
            new SubmitReviewCommand(item.Id, SubmittedAnswer: "The innovation changed everything."),
            CancellationToken.None);

        result.Stage.Should().Be(ReviewStage.Day21);
        result.Passed.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitReview_WrittenUsage_fails_when_the_assessor_rejects_the_sentence()
    {
        var item = Word();
        item.RecordReview(true, Now); // Day3 → Day7
        _vocabulary.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        _wordUsageAssessor
            .AssessAsync(item.Word, item.Translation, "I like apples.", Arg.Any<CancellationToken>())
            .Returns(WordUsageAssessment.Fail(WordUsageReasonCode.WordNotUsed));
        var handler = new SubmitReviewCommandHandler(_vocabulary, _wordUsageAssessor, _clock);

        var result = await handler.Handle(
            new SubmitReviewCommand(item.Id, SubmittedAnswer: "I like apples."), CancellationToken.None);

        result.Stage.Should().Be(ReviewStage.Day3);
        result.FailCount.Should().Be(1);
        result.Passed.Should().BeFalse();
        result.ReasonCode.Should().Be(WordUsageReasonCode.WordNotUsed);
    }

    [Fact]
    public async Task SubmitReview_SpokenUsage_still_trusts_the_learner_self_rating()
    {
        // Day21 → SpokenUsage, out of scope for server verification (needs Azure Speech pronunciation
        // assessment - a separate subsystem); self-rated recall stays a legitimate SRS mechanic.
        var item = Word();
        item.RecordReview(true, Now); // Day3 → Day7
        item.RecordReview(true, Now); // Day7 → Day21
        _vocabulary.GetByIdAsync(item.Id, Arg.Any<CancellationToken>()).Returns(item);
        var handler = new SubmitReviewCommandHandler(_vocabulary, _wordUsageAssessor, _clock);

        var result = await handler.Handle(
            new SubmitReviewCommand(item.Id, SelfRatedPassed: true), CancellationToken.None);

        result.Mastered.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitReview_throws_when_item_missing()
    {
        _vocabulary.GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>()).Returns((VocabularyItem?)null);
        var handler = new SubmitReviewCommandHandler(_vocabulary, _wordUsageAssessor, _clock);

        var act = () => handler.Handle(
            new SubmitReviewCommand(Guid.NewGuid(), SelfRatedPassed: true), CancellationToken.None);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetDueReviews_maps_each_word_to_a_mini_test_type()
    {
        var item = Word();
        _vocabulary.GetDueForLearnerAsync(Learner, Now, Arg.Any<CancellationToken>())
            .Returns(new[] { item });
        _vocabulary.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>()).Returns(new[] { item });
        var handler = new GetDueReviewsQueryHandler(_vocabulary, _clock);

        var due = await handler.Handle(new GetDueReviewsQuery(Learner), CancellationToken.None);

        due.Should().ContainSingle();
        due[0].MiniTestType.Should().Be(MiniTestType.ClozeChoice);
        // Only one word on the account: no distractors to draw from, so the option list is just
        // the correct word itself.
        due[0].Options.Should().BeEquivalentTo(new[] { item.Word });
    }

    [Fact]
    public async Task GetDueReviews_builds_cloze_options_from_the_learners_other_words()
    {
        var target = Word("innovation");
        var distractor1 = Word("perspective");
        var distractor2 = Word("resilience");
        _vocabulary.GetDueForLearnerAsync(Learner, Now, Arg.Any<CancellationToken>())
            .Returns(new[] { target });
        _vocabulary.GetByLearnerIdAsync(Learner, Arg.Any<CancellationToken>())
            .Returns(new[] { target, distractor1, distractor2 });
        var handler = new GetDueReviewsQueryHandler(_vocabulary, _clock);

        var due = await handler.Handle(new GetDueReviewsQuery(Learner), CancellationToken.None);

        due[0].Options.Should().NotBeNull();
        due[0].Options.Should().Contain("innovation");
        due[0].Options!.Count.Should().BeGreaterThan(1);
        due[0].Options.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public async Task Dispatch_sends_one_single_word_notification_per_learner()
    {
        var item = Word("perspective");
        _vocabulary.GetDueAsync(Now, Arg.Any<CancellationToken>()).Returns(new[] { item });

        var dispatcher = Substitute.For<INotificationDispatcher>();
        var templates = Substitute.For<INotificationTemplateProvider>();
        templates.Get(NotificationCodes.ReviewOne, Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns("Bugun takrorlash uchun bitta so'z tayyor: 'perspective'.");
        var handler = new DispatchDueReviewNotificationsCommandHandler(_vocabulary, dispatcher, templates, Substitute.For<IPushNotifier>(), _clock);

        var result = await handler.Handle(new DispatchDueReviewNotificationsCommand(), CancellationToken.None);

        result.NotifiedLearners.Should().Be(1);
        result.TotalDueItems.Should().Be(1);
        await dispatcher.Received(1).SendAsync(
            Arg.Is<Notification>(n => n.LearnerId == Learner && n.Code == NotificationCodes.ReviewOne),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Dispatch_groups_many_due_words_into_one_count_notification_per_learner()
    {
        var items = new[] { Word("a"), Word("b"), Word("c") };
        _vocabulary.GetDueAsync(Now, Arg.Any<CancellationToken>()).Returns(items);

        var dispatcher = Substitute.For<INotificationDispatcher>();
        var templates = Substitute.For<INotificationTemplateProvider>();
        templates.Get(NotificationCodes.ReviewMany, Arg.Any<IReadOnlyDictionary<string, string>>())
            .Returns("Bugun 3 ta so'zni takrorlash vaqti keldi.");
        var handler = new DispatchDueReviewNotificationsCommandHandler(_vocabulary, dispatcher, templates, Substitute.For<IPushNotifier>(), _clock);

        var result = await handler.Handle(new DispatchDueReviewNotificationsCommand(), CancellationToken.None);

        result.NotifiedLearners.Should().Be(1);
        result.TotalDueItems.Should().Be(3);
        await dispatcher.Received(1).SendAsync(
            Arg.Is<Notification>(n => n.Code == NotificationCodes.ReviewMany), Arg.Any<CancellationToken>());
    }
}
