using Application.Ai;
using Application.Analytics.Ports;
using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Speaking.Common;
using Application.Speaking.Dtos;
using Application.Speaking.Ports;
using Application.Subscription.Entitlements;
using Domain.Analytics;
using Domain.Gamification;
using Domain.Speaking;
using Domain.Subscription;
using MediatR;

namespace Application.Speaking.StartRoleplay;

public sealed class StartRoleplayCommandHandler
    : IRequestHandler<StartRoleplayCommand, StartRoleplayResult>
{
    private readonly IConversationStore _conversations;
    private readonly IConversationTutor _tutor;
    private readonly ITextToSpeechService _tts;
    private readonly IEntitlementService _entitlements;
    private readonly IUserAccountStore _accounts;
    private readonly IProductEventStore _productEvents;
    private readonly ILearnerPointsRepository _points;
    private readonly TimeProvider _clock;
    private readonly IAiFeatureScope _aiScope;
    private readonly ISpeakingCurriculumProvider _curriculum;

    public StartRoleplayCommandHandler(
        IConversationStore conversations,
        IConversationTutor tutor,
        ITextToSpeechService tts,
        IEntitlementService entitlements,
        IUserAccountStore accounts,
        IProductEventStore productEvents,
        ILearnerPointsRepository points,
        TimeProvider clock,
        IAiFeatureScope? aiScope = null,
        ISpeakingCurriculumProvider? curriculum = null)
    {
        _points = points;
        _conversations = conversations;
        _tutor = tutor;
        _tts = tts;
        _entitlements = entitlements;
        _accounts = accounts;
        _productEvents = productEvents;
        _clock = clock;
        _aiScope = aiScope ?? NoOpAiFeatureScope.Instance;
        _curriculum = curriculum ?? new NullCurriculumProvider();
    }

    public async Task<StartRoleplayResult> Handle(
        StartRoleplayCommand request, CancellationToken cancellationToken)
    {
        // A roleplay is a Speaking session: apply the same freemium gate as a normal conversation
        // (Free learners get a limited number per day; Premium is unlimited) - PROJECT-SPEC H.1.
        try
        {
            await _entitlements.EnsureAllowedAsync(
                request.LearnerId, PremiumFeature.SpeakingSession, cancellationToken);
        }
        catch (FeatureLimitExceededException)
        {
            await _productEvents.AppendAsync(
                ProductEvent.Record(
                    request.LearnerId,
                    ProductEventType.PaywallHit,
                    _clock.GetUtcNow(),
                    source: "roleplay-session"),
                cancellationToken);
            throw;
        }

        // The learner's stored preferred name is the only name the persona is ever given; when unset it
        // stays null and the tutor is instructed never to invent one (same rule as free conversation).
        var account = await _accounts.GetByIdAsync(request.LearnerId, cancellationToken);

        // Resolve through the catalog so the session stores the canonical code (and an unknown code -
        // which the validator already rejects - can never reach the tutor prompt).
        var scenario = RoleplayScenarioCatalog.Get(request.ScenarioCode);

        // A roleplay is a Speaking start, so it costs one energy - same rule, same booking key
        // (the session id), charged before the billable tutor call below.
        var sessionId = Guid.NewGuid();
        var energy = await _points.ConsumeEnergyAsync(
            request.LearnerId, EnergyAction.Speaking, sessionId.ToString(), _clock.GetUtcNow(), cancellationToken);
        if (energy.Outcome == EnergyOutcome.Insufficient)
            throw new EnergyExhaustedException(energy.NextRefillAt);

        var curriculum = await _curriculum.MatchAsync(
            request.Level, $"{scenario.EnglishTitle} {scenario.Setting} {scenario.LearnerObjective}", cancellationToken);
        var session = ConversationSession.Start(
            request.LearnerId, request.Level, learnerName: account?.PreferredName,
            scenarioCode: scenario.Code, curriculumContext: curriculum, sessionId: sessionId);

        // The tutor reads the scenario off the session and opens in character. Strip markdown/emoji so
        // neither the bubble nor the spoken audio carries symbols the TTS voice would read aloud.
        using var aiScope = await _aiScope.EnterAsync(AiFeature.SpeakingTutor, request.LearnerId, cancellationToken);
        var rawOpening = await _tutor.NextReplyAsync(session, cancellationToken);
        var opening = SpeechText.Clean(rawOpening);
        if (string.IsNullOrWhiteSpace(opening))
            opening = "Hello. Let's begin.";
        session.AddTutorTurn(opening);

        var speech = await _tts.SynthesizeAsync(opening, cancellationToken);
        await _conversations.SaveAsync(session, cancellationToken);
        await _entitlements.RecordUsageAsync(
            request.LearnerId, PremiumFeature.SpeakingSession, cancellationToken);

        return new StartRoleplayResult(
            session.Id,
            scenario.Code,
            opening,
            Convert.ToBase64String(speech.AudioContent),
            VisemeFrameDto.FromDomain(speech.Visemes),
            speech.Visemes.Animation,
            speech.IsNaturalVoice,
            SpeechWordTimingDto.FromDomain(speech.Timings));
    }

    private sealed class NullCurriculumProvider : ISpeakingCurriculumProvider
    {
        public Task<SpeakingCurriculumContext?> ForTopicAsync(Guid topicId, CancellationToken cancellationToken) => Task.FromResult<SpeakingCurriculumContext?>(null);
        public Task<SpeakingCurriculumContext?> MatchAsync(Domain.Assessment.CefrLevel level, string text, CancellationToken cancellationToken) => Task.FromResult<SpeakingCurriculumContext?>(null);
    }
}
