using Application.Analytics.Ports;
using Application.Common;
using Application.Gamification.Dtos;
using Application.Gamification.Ports;
using Application.Identity.Ports;
using Application.Speaking.Common;
using Application.Speaking.Dtos;
using Application.Speaking.Ports;
using Application.Subscription.Access;
using Application.Subscription.Entitlements;
using Application.Vocabulary.Ports;
using Domain.Analytics;
using Domain.Gamification;
using Domain.Speaking;
using Domain.Subscription;
using Domain.Vocabulary;
using MediatR;

namespace Application.Speaking.StartConversation;

public sealed class StartConversationCommandHandler
    : IRequestHandler<StartConversationCommand, StartConversationResult>
{
    private readonly IConversationStore _conversations;
    private readonly IConversationTutor _tutor;
    private readonly ITextToSpeechService _tts;
    private readonly IEntitlementService _entitlements;
    private readonly IVocabularyTopicRepository _vocabularyTopics;
    private readonly ITopicSpeakingProgressStore _topicProgress;
    private readonly ITopicAccessPolicy _access;
    private readonly IUserAccountStore _accounts;
    private readonly IProductEventStore _productEvents;
    private readonly ISpeakingCurriculumProvider _curriculum;
    private readonly ILearnerPointsRepository _points;
    private readonly TimeProvider _clock;

    public StartConversationCommandHandler(
        IConversationStore conversations,
        IConversationTutor tutor,
        ITextToSpeechService tts,
        IEntitlementService entitlements,
        IVocabularyTopicRepository vocabularyTopics,
        ITopicSpeakingProgressStore topicProgress,
        ITopicAccessPolicy access,
        IUserAccountStore accounts,
        IProductEventStore productEvents,
        ILearnerPointsRepository points,
        TimeProvider clock,
        ISpeakingCurriculumProvider? curriculum = null)
    {
        _points = points;
        _conversations = conversations;
        _tutor = tutor;
        _tts = tts;
        _entitlements = entitlements;
        _vocabularyTopics = vocabularyTopics;
        _topicProgress = topicProgress;
        _access = access;
        _accounts = accounts;
        _productEvents = productEvents;
        _clock = clock;
        _curriculum = curriculum ?? new NullCurriculumProvider();
    }

    public async Task<StartConversationResult> Handle(
        StartConversationCommand request,
        CancellationToken cancellationToken)
    {
        // Freemium gating (PROJECT-SPEC H.1): Free learners get a limited number of
        // speaking sessions per day; Premium is unlimited.
        try
        {
            await _entitlements.EnsureAllowedAsync(
                request.LearnerId, PremiumFeature.SpeakingSession, cancellationToken);
        }
        catch (FeatureLimitExceededException)
        {
            await RecordPaywallAsync(request.LearnerId, "speaking-session", cancellationToken);
            throw;
        }

        // When the learner came from a vocabulary topic, anchor the conversation on that
        // topic's English title and feed its target words to the tutor (resolved here so the
        // word list is server-authoritative, not trusted from the client). Falls back to the
        // curated Topic code. An empty/unfilled topic just yields the title with no words.
        var (topic, focusWords, topicId) = await ResolveTopicAsync(request, cancellationToken);

        // Trial paywall (H.1): a topic-anchored speaking session beyond the free allowance requires
        // Premium. Free-form sessions (no topic) are gated by the per-day SpeakingSession limit above.
        if (topicId is { } anchoredTopicId)
        {
            try
            {
                await _access.EnsureLearningAccessAsync(
                    request.LearnerId, anchoredTopicId, Domain.Learning.SkillType.Speaking, cancellationToken);
            }
            catch (SubscriptionRequiredException)
            {
                await RecordPaywallAsync(request.LearnerId, anchoredTopicId.ToString(), cancellationToken);
                throw;
            }
        }

        // The learner's stored preferred name is the only name the tutor is ever given. When the
        // learner hasn't set one it stays null and the tutor is instructed never to invent a name.
        var account = await _accounts.GetByIdAsync(request.LearnerId, cancellationToken);
        var learnerName = account?.PreferredName;

        // Energy gate: starting a conversation costs one unit. Booked against the session id we
        // mint here, before the (billable) tutor call, so a blocked start spends neither energy
        // nor LLM tokens and a reconnect to this same session is never charged twice.
        var sessionId = Guid.NewGuid();
        var energy = await _points.ConsumeEnergyAsync(
            request.LearnerId, EnergyAction.Speaking, sessionId.ToString(), _clock.GetUtcNow(), cancellationToken);
        if (energy.Outcome == EnergyOutcome.Insufficient)
            throw new EnergyExhaustedException(energy.NextRefillAt);

        var curriculum = topicId is { } resolvedTopicId
            ? await _curriculum.ForTopicAsync(resolvedTopicId, cancellationToken)
            : null;
        var session = ConversationSession.Start(
            request.LearnerId, request.Level, topic, focusWords, topicId, learnerName,
            curriculumContext: curriculum, sessionId: sessionId);

        // Strip markdown markers, emojis and stickers so neither the bubble nor the spoken
        // audio carry symbols the TTS voice would read aloud (docs/development-guide.md rule on AI output).
        var rawOpening = await _tutor.NextReplyAsync(session, cancellationToken);
        var opening = SpeechText.Clean(rawOpening);
        if (string.IsNullOrWhiteSpace(opening))
            opening = "Hello! What would you like to talk about today?";
        session.AddTutorTurn(opening);

        var speech = await _tts.SynthesizeAsync(opening, cancellationToken);
        await _conversations.SaveAsync(session, cancellationToken);
        await _entitlements.RecordUsageAsync(
            request.LearnerId, PremiumFeature.SpeakingSession, cancellationToken);

        if (topicId is not null || request.Topic is not null)
            await _productEvents.AppendOnceAsync(
                request.LearnerId,
                ProductEventType.TopicOpened,
                _clock.GetUtcNow(),
                source: (topicId?.ToString() ?? request.Topic),
                cancellationToken);

        var topicProgress = await LoadTopicProgressAsync(request.LearnerId, topicId, cancellationToken);

        return new StartConversationResult(
            session.Id,
            opening,
            Convert.ToBase64String(speech.AudioContent),
            VisemeFrameDto.FromDomain(speech.Visemes),
            speech.Visemes.Animation,
            topicProgress,
            speech.IsNaturalVoice,
            SpeechWordTimingDto.FromDomain(speech.Timings));
    }

    private sealed class NullCurriculumProvider : ISpeakingCurriculumProvider
    {
        public Task<SpeakingCurriculumContext?> ForTopicAsync(Guid topicId, CancellationToken cancellationToken) => Task.FromResult<SpeakingCurriculumContext?>(null);
        public Task<SpeakingCurriculumContext?> MatchAsync(Domain.Assessment.CefrLevel level, string text, CancellationToken cancellationToken) => Task.FromResult<SpeakingCurriculumContext?>(null);
    }

    private async Task RecordPaywallAsync(Guid learnerId, string source, CancellationToken cancellationToken) =>
        await _productEvents.AppendAsync(
            ProductEvent.Record(learnerId, ProductEventType.PaywallHit, _clock.GetUtcNow(), source),
            cancellationToken);

    private async Task<(string? Topic, IReadOnlyList<string> FocusWords, Guid? TopicId)> ResolveTopicAsync(
        StartConversationCommand request, CancellationToken cancellationToken)
    {
        if (Guid.TryParse(request.VocabularyTopicId, out var topicId) && topicId != Guid.Empty)
        {
            var vocabularyTopic = await _vocabularyTopics.GetByIdAsync(topicId, cancellationToken);
            if (vocabularyTopic is not null)
            {
                var words = vocabularyTopic.Words.Select(w => w.Word).ToList();
                return (vocabularyTopic.Title, words, vocabularyTopic.Id);
            }
        }

        return (request.Topic, Array.Empty<string>(), null);
    }

    private async Task<TopicSpeakingProgressDto?> LoadTopicProgressAsync(
        Guid learnerId, Guid? topicId, CancellationToken cancellationToken)
    {
        if (topicId is not { } id)
            return null;

        var progress = await _topicProgress.GetAsync(learnerId, id, cancellationToken);
        var spokenSeconds = (int)Math.Round((progress?.SpokenTime ?? TimeSpan.Zero).TotalSeconds);
        return new TopicSpeakingProgressDto(
            spokenSeconds,
            (int)TopicSpeakingProgress.RequiredSpeakingTime.TotalSeconds,
            progress?.IsLearned ?? false,
            JustLearned: false);
    }
}
