using Application.Analytics.Ports;
using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Speaking.StartConversation;
using Application.Subscription.Entitlements;
using Application.Tests.Common;
using Application.Vocabulary.Ports;
using Domain.Assessment;
using Domain.Gamification;
using Domain.Identity;
using Domain.Speaking;
using Domain.Vocabulary;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class StartConversationCommandHandlerTests
{
    private readonly IConversationStore _conversations = Substitute.For<IConversationStore>();
    private readonly IConversationTutor _tutor = Substitute.For<IConversationTutor>();
    private readonly ITextToSpeechService _tts = Substitute.For<ITextToSpeechService>();
    private readonly IEntitlementService _entitlements = Substitute.For<IEntitlementService>();
    private readonly IVocabularyTopicRepository _vocabularyTopics =
        Substitute.For<IVocabularyTopicRepository>();
    private readonly ITopicSpeakingProgressStore _topicProgress =
        Substitute.For<ITopicSpeakingProgressStore>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();
    private readonly ILearnerPointsRepository _points = Substitute.For<ILearnerPointsRepository>();

    private StartConversationCommandHandler CreateHandler()
    {
        _points.ConsumeEnergyAsync(
                Arg.Any<Guid>(), EnergyAction.Speaking, Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new EnergyBalance(4, DateTimeOffset.UtcNow.AddMinutes(36), DateTimeOffset.UtcNow.AddMinutes(144), EnergyOutcome.Consumed));
        return new(_conversations, _tutor, _tts, _entitlements, _vocabularyTopics, _topicProgress,
            TopicAccessTestDoubles.AllowAll(), _accounts, _events, _points, TimeProvider.System);
    }

    private static SynthesizedSpeech Speech() =>
        new(new byte[] { 1, 2, 3 },
            VisemeSequence.Create(new[] { new VisemeFrame(1, TimeSpan.Zero) }, TimeSpan.FromMilliseconds(120)),
            IsNaturalVoice: false,
            WordTimings: new[]
            {
                new SpeechWordTiming("Hello", 0, 5, TimeSpan.Zero, TimeSpan.FromMilliseconds(120)),
            });

    [Fact]
    public async Task Handle_greets_synthesizes_and_persists_session()
    {
        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>())
            .Returns("Hello! What is your name?");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        var handler = CreateHandler();

        var result = await handler.Handle(
            new StartConversationCommand(Guid.NewGuid(), CefrLevel.A2), CancellationToken.None);

        result.SessionId.Should().NotBeEmpty();
        result.TutorText.Should().Be("Hello! What is your name?");
        result.TutorAudioBase64.Should().Be(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        result.Visemes.Should().HaveCount(1);
        result.IsNaturalVoice.Should().BeFalse();
        result.WordTimings.Should().ContainSingle().Which.Text.Should().Be("Hello");
        await _conversations.Received(1).SaveAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_passes_the_chosen_topic_to_the_persisted_session()
    {
        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>())
            .Returns("Hello! Let's talk about travel.");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        ConversationSession? saved = null;
        await _conversations.SaveAsync(
            Arg.Do<ConversationSession>(s => saved = s), Arg.Any<CancellationToken>());

        var handler = CreateHandler();

        await handler.Handle(
            new StartConversationCommand(Guid.NewGuid(), CefrLevel.A2, "travel"), CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.Topic.Should().Be("travel");
    }

    [Fact]
    public async Task Handle_seeds_topic_and_focus_words_from_a_linked_vocabulary_topic()
    {
        var topic = VocabularyTopic.Curate(
            "a2-a-busy-morning", "A Busy Morning", "Bandlik tongi", "daily_life", "past-simple",
            CefrLevel.A2, DateTimeOffset.UtcNow);
        topic.FillContent(
            "A short passage about a busy morning.",
            new[]
            {
                TopicWord.Create("hurry", "shoshilmoq", "I hurry to catch the bus."),
                TopicWord.Create("breakfast", "nonushta", "I eat breakfast at seven."),
            });
        _vocabularyTopics.GetByIdAsync(topic.Id, Arg.Any<CancellationToken>()).Returns(topic);

        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>())
            .Returns("Hello! Let's talk about a busy morning.");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        ConversationSession? saved = null;
        await _conversations.SaveAsync(
            Arg.Do<ConversationSession>(s => saved = s), Arg.Any<CancellationToken>());

        var handler = CreateHandler();

        var result = await handler.Handle(
            new StartConversationCommand(
                Guid.NewGuid(), CefrLevel.A2, VocabularyTopicId: topic.Id.ToString()),
            CancellationToken.None);

        saved.Should().NotBeNull();
        // The conversation is anchored on the topic's English title, and the tutor is given
        // the words the learner just studied so it can encourage their use.
        saved!.Topic.Should().Be("A Busy Morning");
        saved.FocusWords.Should().Equal("hurry", "breakfast");
        // The session is linked to the topic so speaking time can be credited toward learning it.
        saved.VocabularyTopicId.Should().Be(topic.Id);
        result.TopicProgress.Should().NotBeNull();
        result.TopicProgress!.GoalSeconds.Should().Be(300);
        result.TopicProgress.Learned.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_seeds_the_session_with_the_learners_stored_preferred_name()
    {
        var learnerId = Guid.NewGuid();
        var account = UserAccount.Register("sub-1", "a@b.com", "Aziz Karimov", null, DateTimeOffset.UtcNow);
        account.SetPreferredName("Aziz");
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns(account);

        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>()).Returns("Hi Aziz!");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        ConversationSession? saved = null;
        await _conversations.SaveAsync(
            Arg.Do<ConversationSession>(s => saved = s), Arg.Any<CancellationToken>());

        await CreateHandler().Handle(
            new StartConversationCommand(learnerId, CefrLevel.A2), CancellationToken.None);

        saved.Should().NotBeNull();
        // The tutor must receive the stored name (never invent one) so it addresses the learner correctly.
        saved!.LearnerName.Should().Be("Aziz");
    }

    [Fact]
    public async Task Handle_leaves_the_session_name_null_when_the_learner_has_not_set_one()
    {
        // No preferred name stored: the session carries no name and the tutor is told not to invent one.
        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>()).Returns("Hello!");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        ConversationSession? saved = null;
        await _conversations.SaveAsync(
            Arg.Do<ConversationSession>(s => saved = s), Arg.Any<CancellationToken>());

        await CreateHandler().Handle(
            new StartConversationCommand(Guid.NewGuid(), CefrLevel.A2), CancellationToken.None);

        saved!.LearnerName.Should().BeNull();
    }

    [Fact]
    public async Task Handle_returns_no_topic_progress_for_a_free_conversation()
    {
        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>())
            .Returns("Hello!");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        var result = await CreateHandler().Handle(
            new StartConversationCommand(Guid.NewGuid(), CefrLevel.A2), CancellationToken.None);

        result.TopicProgress.Should().BeNull();
    }

    [Fact]
    public async Task Handle_with_no_energy_throws_before_creating_a_session()
    {
        var handler = CreateHandler();
        _points.ConsumeEnergyAsync(
                Arg.Any<Guid>(), EnergyAction.Speaking, Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new EnergyBalance(0, DateTimeOffset.UtcNow.AddMinutes(36), DateTimeOffset.UtcNow.AddHours(3), EnergyOutcome.Insufficient));

        var act = () => handler.Handle(
            new StartConversationCommand(Guid.NewGuid(), CefrLevel.A2), CancellationToken.None);

        await act.Should().ThrowAsync<EnergyExhaustedException>();
        await _conversations.DidNotReceive().SaveAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>());
    }
}
