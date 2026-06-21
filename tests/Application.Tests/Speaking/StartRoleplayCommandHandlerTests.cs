using Application.Analytics.Ports;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Speaking.Models;
using Application.Speaking.Ports;
using Application.Speaking.StartRoleplay;
using Application.Subscription.Entitlements;
using Domain.Assessment;
using Domain.Gamification;
using Domain.Identity;
using Domain.Speaking;
using Domain.Subscription;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Application.Tests.Speaking;

public class StartRoleplayCommandHandlerTests
{
    private readonly IConversationStore _conversations = Substitute.For<IConversationStore>();
    private readonly IConversationTutor _tutor = Substitute.For<IConversationTutor>();
    private readonly ITextToSpeechService _tts = Substitute.For<ITextToSpeechService>();
    private readonly IEntitlementService _entitlements = Substitute.For<IEntitlementService>();
    private readonly IUserAccountStore _accounts = Substitute.For<IUserAccountStore>();
    private readonly IProductEventStore _events = Substitute.For<IProductEventStore>();
    private readonly ILearnerPointsRepository _points = Substitute.For<ILearnerPointsRepository>();

    private StartRoleplayCommandHandler CreateHandler()
    {
        _points.ConsumeEnergyAsync(
                Arg.Any<Guid>(), EnergyAction.Speaking, Arg.Any<string>(), Arg.Any<DateTimeOffset>(), Arg.Any<CancellationToken>())
            .Returns(new EnergyBalance(4, DateTimeOffset.UtcNow.AddMinutes(36), DateTimeOffset.UtcNow.AddMinutes(144), EnergyOutcome.Consumed));
        return new(_conversations, _tutor, _tts, _entitlements, _accounts, _events, _points, TimeProvider.System);
    }

    private static SynthesizedSpeech Speech() =>
        new(new byte[] { 1, 2, 3 },
            VisemeSequence.Create(new[] { new VisemeFrame(1, TimeSpan.Zero) }, TimeSpan.FromMilliseconds(120)),
            IsNaturalVoice: false,
            WordTimings: new[]
            {
                new SpeechWordTiming("Good", 0, 4, TimeSpan.Zero, TimeSpan.FromMilliseconds(120)),
            });

    [Fact]
    public async Task Handle_persists_a_roleplay_session_with_the_chosen_scenario()
    {
        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>())
            .Returns("Good evening, welcome! Can I get you something to drink?");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        ConversationSession? saved = null;
        await _conversations.SaveAsync(
            Arg.Do<ConversationSession>(s => saved = s), Arg.Any<CancellationToken>());

        var result = await CreateHandler().Handle(
            new StartRoleplayCommand(Guid.NewGuid(), CefrLevel.A2, "restaurant"),
            CancellationToken.None);

        saved.Should().NotBeNull();
        saved!.IsRoleplay.Should().BeTrue();
        saved.ScenarioCode.Should().Be("restaurant");
        result.ScenarioCode.Should().Be("restaurant");
        result.TutorText.Should().Be("Good evening, welcome! Can I get you something to drink?");
        result.TutorAudioBase64.Should().Be(Convert.ToBase64String(new byte[] { 1, 2, 3 }));
        result.IsNaturalVoice.Should().BeFalse();
        result.WordTimings.Should().ContainSingle().Which.Text.Should().Be("Good");
    }

    [Fact]
    public async Task Handle_gates_and_records_a_speaking_session()
    {
        var learnerId = Guid.NewGuid();
        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>()).Returns("Hello.");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        await CreateHandler().Handle(
            new StartRoleplayCommand(learnerId, CefrLevel.A1, "airport"), CancellationToken.None);

        // A roleplay is a Speaking session for freemium purposes: it must be gated and its usage recorded.
        await _entitlements.Received(1).EnsureAllowedAsync(
            learnerId, PremiumFeature.SpeakingSession, Arg.Any<CancellationToken>());
        await _entitlements.Received(1).RecordUsageAsync(
            learnerId, PremiumFeature.SpeakingSession, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_seeds_the_session_with_the_learners_stored_preferred_name()
    {
        var learnerId = Guid.NewGuid();
        var account = UserAccount.Register("sub-1", "a@b.com", "Aziz Karimov", null, DateTimeOffset.UtcNow);
        account.SetPreferredName("Aziz");
        _accounts.GetByIdAsync(learnerId, Arg.Any<CancellationToken>()).Returns(account);

        _tutor.NextReplyAsync(Arg.Any<ConversationSession>(), Arg.Any<CancellationToken>()).Returns("Hello.");
        _tts.SynthesizeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(Speech());

        ConversationSession? saved = null;
        await _conversations.SaveAsync(
            Arg.Do<ConversationSession>(s => saved = s), Arg.Any<CancellationToken>());

        await CreateHandler().Handle(
            new StartRoleplayCommand(learnerId, CefrLevel.A2, "job_interview"),
            CancellationToken.None);

        saved!.LearnerName.Should().Be("Aziz");
    }
}
