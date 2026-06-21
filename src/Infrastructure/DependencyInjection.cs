using Application.Admin.Ports;
using Application.Ai;
using Application.Assistant.Ports;
using Application.Analytics.Ports;
using Infrastructure.Analytics;
using Infrastructure.Ai;
using Infrastructure.Assistant;
using Infrastructure.Assistant.Sessions;
using Infrastructure.Diagnostics;
using Application.Developer.Ports;
using Infrastructure.Developer;
using Application.Assessment.Ports;
using Application.Backfill.Ports;
using Application.Books.Ports;
using Application.Common;
using Application.Competition.Ports;
using Infrastructure.Backfill;
using Infrastructure.Books;
using Infrastructure.Competition;
using Infrastructure.Curriculum;
using Infrastructure.Images;
using Application.Gamification;
using Application.Gamification.Ports;
using Application.Grammar.Ports;
using Infrastructure.Grammar;
using Application.Learning.Ports;
using Application.Listening.Ports;
using Domain.Content;
using Domain.Subscription;
using Infrastructure.Content;
using Infrastructure.Listening;
using Application.Notifications.Ports;
using Application.Reading.Ports;
using Infrastructure.Reading;
using Application.Referral.Ports;
using Infrastructure.Referral;
using Application.Retention.Ports;
using Infrastructure.Retention;
using Application.Translation.Ports;
using Infrastructure.Translation;
using Application.Speaking.Ports;
using Application.Speaking.AccentTutors;
using Application.Subscription.Access;
using Application.Subscription.Ports;
using Application.Vocabulary.Ports;
using Application.Vocabulary.Admin.Images;
using Application.Writing.Ports;
using Infrastructure.Writing;
using Infrastructure.Assessment;
using Infrastructure.Gamification;
using Infrastructure.Learning;
using Infrastructure.Notifications;
using Infrastructure.Notifications.Fcm;
using Infrastructure.Persistence;
using Application.Video.Ports;
using Infrastructure.Llm;
using Infrastructure.Speaking;
using Infrastructure.Subscription;
using Infrastructure.Video;
using Infrastructure.Vocabulary;
using Infrastructure.RateLimiting;
using Infrastructure.Redis;
using Infrastructure.Storage;
using Infrastructure.Support;
using Application.Support;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Infrastructure;

/// <summary>
/// Registers the Infrastructure layer: the database context and the adapters that
/// implement the Application ports.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Postgres");
        var hasDatabase = !string.IsNullOrWhiteSpace(connectionString);
        var objectStorageEnabled = configuration.GetValue<bool>($"{ObjectStorageOptions.SectionName}:Enabled");

        // The clock is normally registered by AddApplication; register it idempotently here too so
        // Infrastructure singletons that depend on it (e.g. the cached video feed source) resolve even
        // when only AddInfrastructure is wired (as in the DI-selection tests). TryAdd keeps the
        // Application registration authoritative when both run.
        services.TryAddSingleton(TimeProvider.System);
        services.AddObjectStorage(configuration);
        if (hasDatabase)
            services.AddScoped<ISupportConversationRepository, EfSupportConversationRepository>();
        else
            services.AddSingleton<ISupportConversationRepository, InMemorySupportConversationRepository>();
        services.AddScoped<ISupportService, SupportService>();
        if (hasDatabase)
            services.AddScoped<MediaObjectMigrationJob>();
        var aiAdmission = configuration.GetSection(AiAdmissionOptions.SectionName).Get<AiAdmissionOptions>()
                          ?? new AiAdmissionOptions();
        aiAdmission.ValidatePricing();
        services.AddSingleton(aiAdmission);
        var azureSpeech = configuration.GetSection(AzureSpeechOptions.SectionName).Get<AzureSpeechOptions>()
                          ?? new AzureSpeechOptions();
        services.AddSingleton(azureSpeech);
        var accentTutors = configuration.GetSection(AzureAccentTutorOptions.SectionName)
            .Get<AzureAccentTutorOptions>() ?? new AzureAccentTutorOptions();
        services.AddSingleton(accentTutors);
        var voiceLive = configuration.GetSection(AzureVoiceLiveOptions.SectionName)
            .Get<AzureVoiceLiveOptions>() ?? new AzureVoiceLiveOptions();
        voiceLive.Validate();
        services.AddSingleton(voiceLive);
        services.AddSingleton<IVariableCostMeter>(sp => new VariableCostMeter(
            sp.GetRequiredService<AiAdmissionOptions>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetService<IRedisConnectionProvider>(),
            sp.GetService<AzureSpeechOptions>()));
        services.AddSingleton<IAiAdmissionControl, AiAdmissionControl>();
        services.AddSingleton<IAiRequestContextResolver, AiRequestContextResolver>();
        services.AddSingleton<IAiFeatureScope, AiFeatureScope>();

        // Super-admin server-operations page (/admin/server). The recent-log ring buffer is a
        // singleton fed by the Web layer's Serilog sink; TryAdd lets Web register the shared instance
        // it also hands the sink, while tests/DI-only wiring still get a working default. The provider
        // reads the process + backing stores per request, so it is scoped (it resolves the DbContext).
        services.TryAddSingleton<IRecentLogStore>(new InMemoryRecentLogStore());
        services.AddScoped<IServerDiagnosticsProvider, ServerDiagnosticsProvider>();
        services.TryAddSingleton<IServerTelemetryStore, InMemoryServerTelemetryStore>();
        services.AddHostedService<ServerTelemetrySampler>();
        services.AddHostedService<AiPricingStartupCheck>();

        if (hasDatabase)
        {
            var postgresOptions = configuration.GetSection(PostgresOptions.SectionName).Get<PostgresOptions>()
                                  ?? new PostgresOptions();
            PostgresConnectionPolicy.ValidateApplication(connectionString!, postgresOptions);
            var boundedConnectionString = PostgresConnectionPolicy.ApplyPoolBudget(
                connectionString!, postgresOptions.PoolSize);
            services.AddDbContextPool<EnglishAiDbContext>(options =>
                options.UseNpgsql(boundedConnectionString, npgsql =>
                {
                    npgsql.CommandTimeout(Math.Clamp(postgresOptions.CommandTimeoutSeconds, 1, 300));
                    if (postgresOptions.MaxRetryCount > 0)
                    {
                        npgsql.EnableRetryOnFailure(
                            Math.Clamp(postgresOptions.MaxRetryCount, 1, 6),
                            TimeSpan.FromSeconds(Math.Clamp(postgresOptions.MaxRetryDelaySeconds, 1, 30)),
                            null);
                    }
                }), Math.Clamp(postgresOptions.PoolSize, 16, 1024));
            services.AddScoped<IDeveloperApiKeyStore, EfDeveloperApiKeyStore>();
        }
        else
        {
            services.AddSingleton<IDeveloperApiKeyStore, InMemoryDeveloperApiKeyStore>();
        }

        services.AddSingleton<IAccentTutorAgent, AzureAccentTutorAgent>();
        services.AddSingleton<IAccentTutorVoiceService, AccentTutorVoiceService>();
        services.AddSingleton<IVoiceLiveConnectionBroker, AzureVoiceLiveConnectionBroker>();
        services.AddSingleton<IVoiceLiveSessionMeter, AzureVoiceLiveSessionMeter>();

        services.AddSingleton<IDeveloperApiKeyProtector, DeveloperApiKeyProtector>();
        // The public /v1 API preserves EnglishAI-issued eai_* authentication, then proxies to the same
        // internal Hermes gateway used by product features. Reuse Hermes settings unless an operator
        // explicitly supplies DeveloperAi overrides for a separate deployment.
        var developerAiSection = configuration.GetSection(DeveloperAiOptions.SectionName);
        var developerAi = developerAiSection.Get<DeveloperAiOptions>() ?? new DeveloperAiOptions();
        var sharedHermes = configuration.GetSection(HermesGatewayOptions.SectionName).Get<HermesGatewayOptions>()
                           ?? new HermesGatewayOptions();
        if (string.IsNullOrWhiteSpace(developerAiSection["BaseUrl"]))
            developerAi.BaseUrl = sharedHermes.Endpoint;
        if (string.IsNullOrWhiteSpace(developerAiSection["Model"]))
            developerAi.Model = sharedHermes.Model;
        if (developerAi.ResolvedApiKeys.Count == 0 && sharedHermes.IsConfigured)
            developerAi.ApiKeys = new[] { sharedHermes.ResolvedApiKey };
        services.AddSingleton(developerAi);
        services.AddHttpClient("developer-ai", client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(developerAi.RequestTimeoutSeconds, 10, 300)));
        services.AddSingleton<IDeveloperAiGateway, HermesDeveloperAiGateway>();

        // Faza 0 persistence: in-memory adapters. Singletons so the question bank is built
        // once and the synthesized listening audio is cached per process. The in-flight
        // placement *session* store is registered further down: durable in Redis when
        // configured (so a test survives a process restart), in-memory otherwise.
        services.AddSingleton<IPlacementQuestionRepository, InMemoryPlacementQuestionRepository>();
        if (objectStorageEnabled)
            services.AddScoped<IPlacementAudioCache, ObjectPlacementAudioCache>();
        else
            services.AddSingleton<IPlacementAudioCache, InMemoryPlacementAudioCache>();
        // Productive placement stages (Writing/Speaking): curated tasks + the speaking
        // assessor. The speaking assessor reuses the config-gated Speech ports and returns
        // a retryable unavailable outcome when the provider is not configured.
        services.AddSingleton<IPlacementProductiveTaskProvider, InMemoryPlacementProductiveTaskProvider>();
        services.AddSingleton<IPlacementSpeakingAssessor, PlacementSpeakingAssessor>();

        // Faza 2 learner model. Durable EF Core persistence when a database is
        // configured; otherwise the in-memory adapter keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<ILearnerProfileRepository, EfLearnerProfileRepository>();
        else
            services.AddSingleton<ILearnerProfileRepository, InMemoryLearnerProfileRepository>();

        services.AddSingleton<IRecommendationTemplateProvider>(
            _ => JsonRecommendationTemplateProvider.FromEmbeddedResource());

        // Aggregate progress reads reuse the current schema; production summarizes in SQL.
        if (hasDatabase)
            services.AddScoped<IVocabularyStatsReader, EfVocabularyStatsReader>();
        else
            services.AddSingleton<IVocabularyStatsReader, InMemoryVocabularyStatsReader>();

        // Faza 2 analytics: per-day study time powering the progress dashboard's
        // today/week/month/year/all-time totals. Durable EF Core persistence when a database is
        // configured so the year history survives; otherwise the in-memory adapter for dev/tests.
        if (hasDatabase)
        {
            services.AddScoped<IStudyLogStore, EfStudyLogStore>();
            services.AddScoped<IProductEventStore, EfProductEventStore>();
        }
        else
        {
            services.AddSingleton<IStudyLogStore, InMemoryStudyLogStore>();
            services.AddSingleton<IProductEventStore, InMemoryProductEventStore>();
        }

        // Faza 3 vocabulary SRS. Durable EF Core persistence when a database is
        // configured; otherwise the in-memory adapter keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<IVocabularyRepository, EfVocabularyRepository>();
        else
            services.AddSingleton<IVocabularyRepository, InMemoryVocabularyRepository>();

        // Module 4 vocabulary topics (passage + 15 words in context). Durable EF Core persistence
        // when a database is configured; otherwise the in-memory adapter (seeded with the 600-topic
        // catalog) keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<IVocabularyTopicRepository, EfVocabularyTopicRepository>();
        else
            services.AddSingleton<IVocabularyTopicRepository, InMemoryVocabularyTopicRepository>();

        // Speaking practice toward learning each topic (the 5-minute rule). Durable per learner
        // when a database is configured; in-memory otherwise.
        if (hasDatabase)
            services.AddScoped<ITopicSpeakingProgressStore, EfTopicSpeakingProgressStore>();
        else
            services.AddSingleton<ITopicSpeakingProgressStore, InMemoryTopicSpeakingProgressStore>();

        if (hasDatabase)
            services.AddScoped<ISpeakingPracticeWordRepository, EfSpeakingPracticeWordRepository>();
        else
            services.AddSingleton<ISpeakingPracticeWordRepository, InMemorySpeakingPracticeWordRepository>();

        // Per-module topic mastery (K.5): a topic is mastered once all six modules pass.
        // Durable per learner when a database is configured; in-memory otherwise.
        if (hasDatabase)
            services.AddScoped<ITopicCompletionStore, EfTopicCompletionStore>();
        else
            services.AddSingleton<ITopicCompletionStore, InMemoryTopicCompletionStore>();

        // Faza 3 notifications. Production uses a durable shared feed so Web and Worker
        // observe the same notifications; database-free tests keep the in-memory adapter.
        if (hasDatabase)
            services.AddScoped<INotificationDispatcher, EfNotificationDispatcher>();
        else
            services.AddSingleton<INotificationDispatcher, InMemoryNotificationDispatcher>();
        services.AddScoped<INotificationRealtimeNotifier, NullNotificationRealtimeNotifier>();
        services.AddSingleton<INotificationTemplateProvider>(
            _ => JsonNotificationTemplateProvider.FromEmbeddedResource());
        services.AddScoped<DueReviewNotificationJob>();
        services.AddScoped<DailyPlanNotificationJob>();
        services.AddScoped<TopicVocabularyBackfillJob>();

        // Read watermark for "Hammasini o'qish": durable per learner so dismissing the derived
        // SRS/daily reminders survives restarts; in-memory otherwise.
        if (hasDatabase)
            services.AddScoped<INotificationReadStateStore, EfNotificationReadStateStore>();
        else
            services.AddSingleton<INotificationReadStateStore, InMemoryNotificationReadStateStore>();

        // Per-notification dismissal (tapping one notification): durable per (learner, notification)
        // so a dismissed feed item stays gone across restarts; in-memory otherwise.
        if (hasDatabase)
            services.AddScoped<INotificationDismissalStore, EfNotificationDismissalStore>();
        else
            services.AddSingleton<INotificationDismissalStore, InMemoryNotificationDismissalStore>();

        // Super-admin broadcasts and the first-seen arrival times of derived reminders: durable when a
        // database is configured (a broadcast reaches every learner and reminders show a real, stable
        // arrival time across restarts), in-memory otherwise.
        if (hasDatabase)
        {
            services.AddScoped<IAdminBroadcastStore, EfAdminBroadcastStore>();
            services.AddScoped<INotificationArrivalStore, EfNotificationArrivalStore>();
            services.AddScoped<IDeviceTokenStore, EfDeviceTokenStore>();
        }
        else
        {
            services.AddSingleton<IAdminBroadcastStore, InMemoryAdminBroadcastStore>();
            services.AddSingleton<INotificationArrivalStore, InMemoryNotificationArrivalStore>();
            services.AddSingleton<IDeviceTokenStore, InMemoryDeviceTokenStore>();
        }

        // Native push (FCM). When a Firebase service account is configured the real HTTP-v1 sender is
        // wired (status-bar push + app badge, reaching devices even when the app is closed); otherwise a
        // no-op notifier is used so the app runs and is testable without Firebase credentials (docs/development-guide.md
        // rule 10 - external services behind ports, secret-gated like the Azure/LLM adapters).
        var fcm = configuration.GetSection(FcmOptions.SectionName).Get<FcmOptions>() ?? new FcmOptions();
        services.AddSingleton(fcm);
        services.AddHttpClient("fcm");
        if (fcm.IsConfigured)
        {
            var serviceAccountJson = fcm.ResolveServiceAccountJson()!;
            var projectId = ResolveFcmProjectId(fcm, serviceAccountJson);
            services.AddSingleton(sp => new GoogleServiceAccountTokenProvider(
                serviceAccountJson, sp.GetRequiredService<IHttpClientFactory>()));
            services.AddScoped<IPushNotifier>(sp => new FcmPushNotifier(
                sp.GetRequiredService<IDeviceTokenStore>(),
                sp.GetRequiredService<GoogleServiceAccountTokenProvider>(),
                sp.GetRequiredService<IHttpClientFactory>(),
                projectId,
                sp.GetRequiredService<ILogger<FcmPushNotifier>>()));
        }
        else
        {
            services.AddSingleton<IPushNotifier, NullPushNotifier>();
        }

        // Faza 1 Speaking adapters. External AI calls go through ports (docs/development-guide.md
        // rule 10). When Azure Speech / LLM keys are configured the real adapters are
        // wired; otherwise deterministic Local* dev stand-ins are used so the app
        // runs and is testable without secrets.
        // Hermes Agent's internal OpenAI-compatible API is the primary LLM backend for every
        // text-intelligence feature. It runs on loopback in development and on the private Docker
        // network in production; only EnglishAI's authenticated /v1 proxy is public.
        var hermesGateway = configuration.GetSection(HermesGatewayOptions.SectionName).Get<HermesGatewayOptions>()
                            ?? new HermesGatewayOptions();
        services.AddSingleton(hermesGateway);
        var projectAssistant = configuration.GetSection(ProjectAssistantOptions.SectionName).Get<ProjectAssistantOptions>()
                               ?? new ProjectAssistantOptions();
        services.AddSingleton(projectAssistant);
        var solPrimary = configuration.GetSection(SolPrimaryOptions.SectionName).Get<SolPrimaryOptions>()
                         ?? new SolPrimaryOptions();
        services.AddSingleton(solPrimary);
        // A stopped loopback gateway must fail fast instead of freezing a learner-facing request for
        // three minutes. Since the gateway is the only backend, this timeout is the request deadline.
        services.AddHttpClient("hermes-gateway", client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(hermesGateway.RequestTimeoutSeconds, 10, 120)));
        services.AddHttpClient("sol-primary", client =>
            client.Timeout = Timeout.InfiniteTimeSpan);
        services.AddSingleton<HermesGatewayLlmCompletion>(sp =>
            new HermesGatewayLlmCompletion(
                sp.GetRequiredService<IHttpClientFactory>(), hermesGateway,
                sp.GetRequiredService<ILogger<HermesGatewayLlmCompletion>>(),
                maxAttemptsPerModel: 1));
        services.AddSingleton<SolPrimaryLlmCompletion>();
        var aiRouting = configuration.GetSection(AiRoutingOptions.SectionName).Get<AiRoutingOptions>()
                        ?? new AiRoutingOptions();
        services.AddSingleton(aiRouting);
        // The quality-critical path: paid primary first, self-hosted gateway as the safety net.
        services.AddSingleton(sp => new PrimaryFallbackLlmCompletion(
            sp.GetRequiredService<SolPrimaryLlmCompletion>(),
            sp.GetRequiredService<HermesGatewayLlmCompletion>(),
            sp.GetRequiredService<ILogger<PrimaryFallbackLlmCompletion>>()));
        // Routing sits INSIDE the resilient wrapper so both branches share one concurrency limit,
        // queue and circuit breaker. Without SOL configured there is only one backend, so the router
        // would be a no-op indirection and is skipped entirely.
        services.AddSingleton(sp => new ResilientHermesGatewayLlmCompletion(
            solPrimary.IsConfigured
                ? new FeatureRoutedLlmCompletion(
                    premium: sp.GetRequiredService<PrimaryFallbackLlmCompletion>(),
                    // The budget path prefers the self-hosted gateway but must not make it a single
                    // point of failure: its upstream is a free tier that can disappear without notice.
                    budget: new PrimaryFallbackLlmCompletion(
                        sp.GetRequiredService<HermesGatewayLlmCompletion>(),
                        sp.GetRequiredService<SolPrimaryLlmCompletion>(),
                        sp.GetRequiredService<ILogger<PrimaryFallbackLlmCompletion>>()),
                    aiRouting,
                    sp.GetRequiredService<ILogger<FeatureRoutedLlmCompletion>>())
                : sp.GetRequiredService<HermesGatewayLlmCompletion>(),
            hermesGateway,
            sp.GetRequiredService<IAiAdmissionControl>(),
            sp.GetRequiredService<ILogger<ResilientHermesGatewayLlmCompletion>>()));
        services.AddSingleton<LocalProgressInsightGenerator>();
        services.AddSingleton<IProgressInsightGenerator>(sp => hermesGateway.IsConfigured
            ? new HermesProgressInsightGenerator(
                sp.GetRequiredService<ResilientHermesGatewayLlmCompletion>(),
                sp.GetRequiredService<LocalProgressInsightGenerator>(),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<HermesProgressInsightGenerator>>())
            : sp.GetRequiredService<LocalProgressInsightGenerator>());

        // The Hermes gateway is now the ONLY LLM backend (user's decision, 2026-07-28). The third-party
        // free providers that used to sit behind it in a fallback chain - Groq, OpenRouter and Google AI
        // Studio - were removed entirely: their adapters, options, config sections and keys are gone, so
        // no request can reach them and no quota can be spent on them. That deletes a whole class of
        // failure the chain kept producing (rate-limited daily caps, reasoning models returning empty
        // replies, four providers tried in sequence before a learner saw an error) and it costs nothing,
        // because the gateway is self-hosted.
        //
        // With one backend there is nothing to fall back TO, so the provider-level composite is gone as
        // well. ResilientHermesGatewayLlmCompletion is the completion every text-intelligence feature
        // resolves; it already owns the concurrency limit, queue and circuit breaker, and the gateway's
        // own HermesGateway:RequestTimeoutSeconds is now the only deadline - previously an outer
        // per-provider budget cut the gateway off long before its own timeout could apply.
        // When the gateway has no key, features fall back to the deterministic Local* stand-ins below
        // rather than fabricating content (docs/development-guide.md rules 8, 11).
        var contentLlmConfigured = hermesGateway.IsConfigured;
        ILlmCompletion ContentCompletion(IServiceProvider sp) => ContentLlm.Completion(
            sp.GetRequiredService<ResilientHermesGatewayLlmCompletion>());

        services.AddSingleton<IUtteranceSemanticClassifier>(sp =>
            new HermesUtteranceSemanticClassifier(
                contentLlmConfigured ? ContentCompletion(sp) : null,
                sp.GetRequiredService<ILogger<HermesUtteranceSemanticClassifier>>()));
        services.AddSingleton<IUtteranceEndpointDetector, UtteranceEndpointDetector>();

        if (azureSpeech.IsConfigured)
        {
            // Registered as its own concrete type so the ports below can be decorated. Resolving the
            // interface and downcasting would throw the moment anything wraps it.
            services.AddSingleton<AzureSpeechToTextService>();
            services.AddSingleton<AzureTextToSpeechService>();
            var whisperStt = configuration.GetSection(WhisperSttOptions.SectionName).Get<WhisperSttOptions>()
                             ?? new WhisperSttOptions();
            services.AddSingleton(whisperStt);
            if (whisperStt.IsConfigured)
            {
                services.AddHttpClient<WhisperSttService>(client =>
                {
                    client.BaseAddress = new Uri(whisperStt.BaseUrl);
                    client.Timeout = TimeSpan.FromSeconds(Math.Clamp(whisperStt.TimeoutSeconds, 5, 120));
                });
                // Free learners transcribe on our own hardware; Premium keeps the metered provider,
                // whose accuracy and pronunciation scoring are what the subscription buys.
                services.AddSingleton<ISpeechToTextService>(sp => new TieredSpeechToTextService(
                    sp.GetRequiredService<WhisperSttService>(),
                    sp.GetRequiredService<AzureSpeechToTextService>(),
                    sp.GetRequiredService<ILogger<TieredSpeechToTextService>>()));
            }
            else
            {
                services.AddSingleton<ISpeechToTextService>(sp => sp.GetRequiredService<AzureSpeechToTextService>());
            }
            // Streaming stays on the concrete Azure adapter: only the accent-tutor hub uses it, and
            // the sidecar has no streaming contract.
            services.AddSingleton<IStreamingSpeechToTextService>(sp =>
                sp.GetRequiredService<AzureSpeechToTextService>());
            services.AddSingleton<IPronunciationAssessor, AzurePronunciationAssessor>();
            // Repeated phrases (session openings, roleplay openers) are paid for once, platform-wide.
            services.AddSingleton<IVoicedTextToSpeechService>(sp => new CachedTextToSpeechService(
                sp.GetRequiredService<AzureTextToSpeechService>(),
                sp.GetRequiredService<ISynthesizedSpeechCache>(),
                azureSpeech,
                sp.GetRequiredService<IVariableCostMeter>()));
            services.AddSingleton<ITextToSpeechService>(sp =>
                sp.GetRequiredService<IVoicedTextToSpeechService>());
        }
        else
        {
            services.AddSingleton<ISpeechToTextService, LocalSpeechToTextService>();
            services.AddSingleton<IPronunciationAssessor, LocalPronunciationAssessor>();
            services.AddSingleton<ITextToSpeechService, LocalTextToSpeechService>();
        }

        services.AddSingleton<LocalConversationTutor>();
        if (hasDatabase)
            services.AddScoped<ISpeakingCurriculumProvider, SpeakingCurriculumProvider>();
        else
            services.AddSingleton<ISpeakingCurriculumProvider, NullSpeakingCurriculumProvider>();
        if (contentLlmConfigured)
        {
            services.AddSingleton(sp => new HermesConversationTutor(
                ContentCompletion(sp),
                sp.GetRequiredService<ILogger<HermesConversationTutor>>()));
            services.AddSingleton<IConversationTutor, ResilientConversationTutor>();
        }
        else
            services.AddSingleton<IConversationTutor>(sp => sp.GetRequiredService<LocalConversationTutor>());

        // Roleplay end-of-scene scoring. Gateway-backed when its key is configured (quality-critical
        // Speaking module - docs/development-guide.md rule 10), otherwise the deterministic Local stand-in.
        if (contentLlmConfigured)
            services.AddSingleton<IRoleplayEvaluator>(sp => new HermesRoleplayEvaluator(ContentCompletion(sp)));
        else
            services.AddSingleton<IRoleplayEvaluator, LocalRoleplayEvaluator>();

        // "Idea cards" for a stuck learner (blank-page cure). Gateway-backed when its key is configured
        // so the cards fit the live topic/level/turns; otherwise the deterministic universal scaffolds.
        if (contentLlmConfigured)
            services.AddSingleton<IIdeaCardGenerator>(sp => new HermesIdeaCardGenerator(ContentCompletion(sp)));
        else
            services.AddSingleton<IIdeaCardGenerator, LocalIdeaCardGenerator>();

        services.AddSingleton<IPhonemeVisualLibrary, PhonemeVisualLibrary>();
        // Per-word red-lips viseme tracks are synthesized once and reused (rule 10).
        services.AddSingleton<IWordVisemeCache, InMemoryWordVisemeCache>();
        services.AddSingleton<IFeedbackTemplateProvider>(_ => JsonFeedbackTemplateProvider.FromEmbeddedResource());
        services.AddSingleton<IWordExampleProvider>(_ => JsonWordExampleProvider.FromEmbeddedResource());

        // Module 4 vocabulary passage generation (rules 10, 11). Topics are cached in PostgreSQL; on a
        // cache miss the request path lazy-fills and caches the result. The LLM generator runs when a
        // free provider key is configured; if it is missing OR returns no usable content (rate-limited,
        // invalid key, offline), the deterministic Local stand-in guarantees the topic still fills with
        // real, level-appropriate words so the learner never sees an empty/pending topic (rules 8, 11).
        services.AddSingleton<IVocabularyPassageGenerator>(sp =>
        {
            IVocabularyPassageGenerator primary = contentLlmConfigured
                ? new LlmVocabularyPassageGenerator(ContentCompletion(sp))
                : new LocalVocabularyPassageGenerator();
            return new FallbackVocabularyPassageGenerator(primary, new LocalVocabularyPassageGenerator());
        });

        // SRS written-sentence mini-test grading (PROJECT-SPEC B.1): "does this sentence correctly
        // use the target word?" - a cheap content-model check (rule 10), not the premium essay
        // assessor. The LLM grader runs when a free provider key is configured, wrapped so a
        // transient failure falls back to the deterministic offline grader instead of failing the
        // review submission (rules 8, 15); otherwise the offline grader runs alone.
        if (contentLlmConfigured)
            services.AddSingleton<IWordUsageAssessor>(sp => new ResilientWordUsageAssessor(
                new LlmWordUsageAssessor(ContentCompletion(sp)),
                new LocalWordUsageAssessor(),
                sp.GetRequiredService<ILogger<ResilientWordUsageAssessor>>()));
        else
            services.AddSingleton<IWordUsageAssessor, LocalWordUsageAssessor>();

        // Faza 4 Video/Listening. Durable EF Core persistence when a database is
        // configured; otherwise the in-memory adapter (seeded with the curated catalog)
        // keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<IVideoRepository, EfVideoRepository>();
        else
            services.AddSingleton<IVideoRepository, InMemoryVideoRepository>();

        services.AddSingleton<IVideoFeedbackStore, InMemoryVideoFeedbackStore>();
        services.AddSingleton<IVideoContentProvider>(_ => JsonVideoContentProvider.FromEmbeddedResource());

        // YouTube ingestion + CEFR leveling go through ports (docs/development-guide.md rules 10, 17.3).
        // Real adapters when keys are configured; otherwise deterministic Local stand-ins
        // so ingestion runs and is testable without secrets.
        var youTube = configuration.GetSection(YouTubeOptions.SectionName).Get<YouTubeOptions>()
                      ?? new YouTubeOptions();
        services.AddSingleton(youTube);

        if (youTube.IsConfigured)
            services.AddHttpClient<IYouTubeMetadataProvider, YouTubeMetadataProvider>();
        else
            services.AddSingleton<IYouTubeMetadataProvider, LocalYouTubeMetadataProvider>();

        // Adaptive video feed (PROJECT-SPEC B.3, Bosqich 2): official Data API when a key is
        // configured, otherwise the keyless public-search adapter (docs/development-guide.md rule 10). Either way it
        // is wrapped in a caching decorator so the same level-keyed pages serve many concurrent
        // learners from one fetch - the free Data API quota (~100 searches/day) then covers 10k+
        // users, and the keyless scraper hits YouTube far less often (rule 10). The concrete source is
        // a typed HttpClient resolved per call from a fresh scope (factory-managed client; no leak).
        var feedCacheTtl = TimeSpan.FromHours(6);
        services.AddHttpClient<YouTubePlaylistDiscoverySource>();
        services.AddScoped<IVideoPlaylistDiscoverySource, YouTubePlaylistDiscoverySource>();

        if (youTube.IsConfigured)
        {
            services.AddHttpClient<YouTubeDataApiFeedSource>();
            services.AddSingleton<IVideoFeedSource>(sp => new CachingVideoFeedSource(
                sp.GetRequiredService<IServiceScopeFactory>(),
                p => p.GetRequiredService<YouTubeDataApiFeedSource>(),
                sp.GetRequiredService<TimeProvider>(),
                feedCacheTtl));
        }
        else
        {
            services.AddHttpClient<LocalYouTubeFeedSource>();
            services.AddSingleton<IVideoFeedSource>(sp => new CachingVideoFeedSource(
                sp.GetRequiredService<IServiceScopeFactory>(),
                p => p.GetRequiredService<LocalYouTubeFeedSource>(),
                sp.GetRequiredService<TimeProvider>(),
                feedCacheTtl));
        }

        // Interactive transcript (PROJECT-SPEC B.3, Bosqich 3): captions are fetched via yt-dlp,
        // which reliably returns them for any captioned video - YouTube's direct timedtext/
        // get_transcript endpoints are IP-blocked (HTTP 200 / 0 bytes) for most server and many
        // residential hosts (verified). Resilient - returns empty (honest "pending") when the
        // tool is missing or the video has no captions, so no key gating is needed (rules 8, 11).
        // ── Transcript fallback chain (PROJECT-SPEC B.3, Bosqich 3) ─────────────────────────────────
        // A single ordered chain managed by TranscriptOrchestrator. Each provider is its own class and
        // is added to the chain only when it can run; adding a new source later is one new class + one
        // list entry, no change to the others. Order (the orchestrator tries each in turn, falling
        // through on any failure):
        //   1. youtubei.js sidecar   - fast, key-less; first when its BaseUrl is configured
        //   2. yt-dlp (cookie-less)  - always present; the free player_client/impersonate bot-wall bypass
        //   3. yt-dlp (cookie-backed)- when cookies are configured; with rotate-and-retry on a block
        //   4. Supadata managed API  - last resort when a key is configured (fetches from an unblocked IP)
        // Nothing is cached or persisted here - every fetch is real-time (docs/development-guide.md rules 8, 10).
        var ytDlp = configuration.GetSection(YtDlpOptions.SectionName).Get<YtDlpOptions>()
                    ?? new YtDlpOptions();
        services.AddSingleton(ytDlp);

        // youtubei.js sidecar (step 1). Config-gated on its BaseUrl; the typed HttpClient is
        // factory-managed so the transient orchestrator never captures a stale handler.
        var youtubei = configuration.GetSection(YoutubeiOptions.SectionName).Get<YoutubeiOptions>()
                       ?? new YoutubeiOptions();
        services.AddSingleton(youtubei);
        if (youtubei.IsConfigured)
            services.AddHttpClient<YoutubeiTranscriptProvider>(http =>
                http.Timeout = TimeSpan.FromSeconds(Math.Max(5, youtubei.TimeoutSeconds)));

        // Central cookie manager (step 3's cookie source). Bound from "YouTubeCookies"; back-compat:
        // the legacy single YtDlp:CookiesPath seeds the pool when no dedicated cookie config is set.
        var cookieOptions = configuration.GetSection(CookieOptions.SectionName).Get<CookieOptions>()
                            ?? new CookieOptions();
        if (string.IsNullOrWhiteSpace(cookieOptions.Path) && !string.IsNullOrWhiteSpace(ytDlp.CookiesPath))
            cookieOptions.Path = ytDlp.CookiesPath;
        services.AddSingleton(cookieOptions);
        services.AddSingleton<ICookieManager, CookieManager>();

        // Supadata managed API (step 4 / last). Typed HttpClient registered only when a key is present.
        var supadata = configuration.GetSection(SupadataOptions.SectionName).Get<SupadataOptions>()
                       ?? new SupadataOptions();
        services.AddSingleton(supadata);
        if (supadata.IsConfigured)
            services.AddHttpClient<SupadataTranscriptProvider>(http =>
                http.Timeout = TimeSpan.FromSeconds(Math.Max(5, supadata.TimeoutSeconds)));

        // Build the ordered chain per resolve (transient) so typed HttpClients stay factory-managed and
        // are never captured by a singleton. yt-dlp instances are constructed here (two variants from the
        // one class: cookie-less and cookie-backed), the rest resolved from the container.
        services.AddTransient<IVideoTranscriptProvider>(sp =>
        {
            var ytDlpLogger = sp.GetRequiredService<ILogger<YtDlpTranscriptProvider>>();
            var chain = new List<IVideoTranscriptProvider>();

            if (youtubei.IsConfigured)
                chain.Add(sp.GetRequiredService<YoutubeiTranscriptProvider>());

            chain.Add(new YtDlpTranscriptProvider(ytDlp, ytDlpLogger));

            if (cookieOptions.IsConfigured)
            {
                var cookies = sp.GetRequiredService<ICookieManager>();
                var cookieYtDlp = new YtDlpTranscriptProvider(ytDlp, ytDlpLogger, cookies);
                chain.Add(new CookieRefreshingTranscriptProvider(
                    cookieYtDlp, cookies, sp.GetRequiredService<ILogger<CookieRefreshingTranscriptProvider>>()));
            }

            if (supadata.IsConfigured)
                chain.Add(sp.GetRequiredService<SupadataTranscriptProvider>());

            // A per-provider backstop timeout (each provider also caps itself) so one hung source never
            // stalls the whole chain.
            return new TranscriptOrchestrator(
                chain, sp.GetRequiredService<ILogger<TranscriptOrchestrator>>(),
                perProviderTimeout: TimeSpan.FromSeconds(120));
        });

        // A video with no caption track settles on the terminal "transcript unavailable" state - we do
        // NOT fall back to recognizing the video's own audio. (An Azure STT fallback was removed on the
        // user's request: captions are the only transcript source for a video lesson.)

        // CEFR leveling runs on free Gemini when configured, otherwise the deterministic Local
        // readability proxy - never Claude (docs/development-guide.md rule 10; Claude is for speaking/writing only).
        if (contentLlmConfigured)
            services.AddSingleton<ICefrVideoLeveler>(sp => new LlmCefrVideoLeveler(ContentCompletion(sp)));
        else
            services.AddSingleton<ICefrVideoLeveler, LocalCefrVideoLeveler>();

        // Uzbek translation of a fetched transcript (PROJECT-SPEC B.3, Bosqich 3): the free Gemini
        // translator when its key is configured, otherwise the honest no-op (transcript stays
        // English-only "pending" rather than fabricating Uzbek - docs/development-guide.md rules 8, 11). Never Claude.
        if (contentLlmConfigured)
            services.AddSingleton<IVideoTranscriptTranslator>(sp => new LlmTranscriptTranslator(ContentCompletion(sp)));
        else
            services.AddSingleton<IVideoTranscriptTranslator, LocalTranscriptTranslator>();

        services.AddScoped<VideoIngestionJob>();

        // Fills a lesson's transcript off the request path so opening a video returns
        // immediately (singleton: it dedupes in-flight fills and opens its own DI scope).
        services.AddSingleton<IVideoTranscriptFiller, BackgroundVideoTranscriptFiller>();

        // Faza 5 gamification (daily goal + streak). Redis (Sorted Sets) is the durable
        // production store; when no Redis connection is configured the in-memory adapter
        // keeps the app runnable in dev/tests (docs/development-guide.md rule 10 config-gating pattern).
        var redis = configuration.GetSection(RedisOptions.SectionName).Get<RedisOptions>()
                    ?? new RedisOptions();
        services.AddSingleton(redis);

        if (redis.IsConfigured)
        {
            services.AddSingleton<IRedisConnectionProvider, RedisConnectionProvider>();
            services.AddSingleton<IDistributedRateLimitStore, RedisDistributedRateLimitStore>();
            services.AddSingleton<IGamificationStore, RedisGamificationStore>();
            services.AddSingleton<IUsageCounter, RedisUsageCounter>();
            services.AddSingleton<IMeteredAllowanceStore, RedisMeteredAllowanceStore>();
            // Durable placement sessions: an in-flight test survives a process restart so the
            // learner never loses progress mid-assessment (Faza 0 in-memory store superseded).
            services.AddSingleton<IPlacementSessionStore, RedisPlacementSessionStore>();
            // Leaderboard/points feature: one Sorted Set per CEFR level, the natural extension
            // of the streak Sorted Set above.
            services.AddSingleton<ILeaderboardStore, RedisLeaderboardStore>();
            services.AddSingleton<IVideoExplainCache, RedisVideoExplainCache>();
            services.AddSingleton<IConversationStore, RedisConversationStore>();
            services.AddSingleton<IVoiceLiveSessionStore, RedisVoiceLiveSessionStore>();
            // Shared so a second app instance never re-buys a line the first already synthesized,
            // with a process-local tier in front to skip the round-trip on the hottest phrases.
            services.AddSingleton<ISynthesizedSpeechCache>(sp => new TieredSynthesizedSpeechCache(
                new InMemorySynthesizedSpeechCache(),
                new RedisSynthesizedSpeechCache(sp.GetRequiredService<IRedisConnectionProvider>())));
        }
        else
        {
            services.AddSingleton<IDistributedRateLimitStore, UnavailableDistributedRateLimitStore>();
            services.AddSingleton<IGamificationStore, InMemoryGamificationStore>();
            services.AddSingleton<IUsageCounter, InMemoryUsageCounter>();
            services.AddSingleton<IMeteredAllowanceStore, InMemoryMeteredAllowanceStore>();
            services.AddSingleton<IPlacementSessionStore, InMemoryPlacementSessionStore>();
            services.AddSingleton<ILeaderboardStore, InMemoryLeaderboardStore>();
            services.AddSingleton<IVideoExplainCache, InMemoryVideoExplainCache>();
            services.AddSingleton<IConversationStore, InMemoryConversationStore>();
            services.AddSingleton<IVoiceLiveSessionStore, InMemoryVoiceLiveSessionStore>();
            services.AddSingleton<ISynthesizedSpeechCache, InMemorySynthesizedSpeechCache>();
        }

        // Faza 5 subscription/payment (PROJECT-SPEC Qism H). Durable EF Core persistence
        // when a database is configured; otherwise in-memory adapters for dev/tests.
        if (hasDatabase)
        {
            services.AddScoped<ISubscriptionRepository, EfSubscriptionRepository>();
            services.AddScoped<IPaymentRepository, EfPaymentRepository>();
            services.AddScoped<IPremiumWaitlistRepository, EfPremiumWaitlistRepository>();
        }
        else
        {
            services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();
            services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();
            services.AddSingleton<IPremiumWaitlistRepository, InMemoryPremiumWaitlistRepository>();
        }

        // Leaderboard/points feature: the points ledger and discount coupons must never be
        // lost (unlike the Redis-only streak counters), so they are always durable EF Core when
        // a database is configured, and in-memory only for dev/tests without one.
        if (hasDatabase)
        {
            services.AddScoped<ILearnerPointsRepository, EfLearnerPointsRepository>();
            services.AddScoped<IDiscountRedemptionRepository, EfDiscountRedemptionRepository>();
        }
        else
        {
            services.AddSingleton<ILearnerPointsRepository, InMemoryLearnerPointsRepository>();
            services.AddSingleton<IDiscountRedemptionRepository, InMemoryDiscountRedemptionRepository>();
        }
        services.AddScoped<RebuildLeaderboardService>();

        // Referral programme storage (accounts + links). Durable EF Core persistence when a
        // database is configured; otherwise the in-memory adapter for dev/tests.
        if (hasDatabase)
            services.AddScoped<IReferralStore, EfReferralStore>();
        else
            services.AddSingleton<IReferralStore, InMemoryReferralStore>();

        services.Configure<RegionalPaymentOptions>(configuration.GetSection(RegionalPaymentOptions.SectionName));
        services.AddSingleton<IPaymentGateway, LocalPaymentGateway>();
        services.AddSingleton<IPaymentGateway>(sp => new RegionalPaymentGateway(
            PaymentProvider.Click,
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RegionalPaymentOptions>>()));
        services.AddSingleton<IPaymentGateway>(sp => new RegionalPaymentGateway(
            PaymentProvider.Payme,
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RegionalPaymentOptions>>()));
        services.AddSingleton<IPaymentGateway>(sp => new RegionalPaymentGateway(
            PaymentProvider.Uzum,
            sp.GetRequiredService<Microsoft.Extensions.Options.IOptions<RegionalPaymentOptions>>()));
        services.AddSingleton<IPaymentGatewayResolver, PaymentGatewayResolver>();
        services.AddSingleton<IPaymentWebhookVerifier, PaymentWebhookVerifier>();
        services.AddSingleton<IClickShopApiVerifier, ClickShopApiVerifier>();
        services.AddScoped<Application.Subscription.ConfirmPayment.ConfirmPaymentCommandHandler>();

        // Online payments stay OFF until a real Click/Payme gateway is configured: only the free
        // plan is sold, and the upgrade flow is refused server-side (PaymentsUnavailableException).
        var paymentsEnabled = configuration.GetValue<bool>(ConfigPaymentAvailability.ConfigKey);
        services.AddSingleton<IPaymentAvailability>(new ConfigPaymentAvailability(paymentsEnabled));
        // Keeps pre-checkout learners on full access until there is a way to pay; self-retiring.
        services.AddSingleton(
            configuration.GetSection(LegacyAccessOptions.SectionName).Get<LegacyAccessOptions>()
            ?? new LegacyAccessOptions());

        services.AddScoped<SubscriptionExpiryJob>();
        services.AddScoped<Infrastructure.Identity.TrialExpiryJob>();

        // Faza 6 reading (leveled passages + interactive glossary). Durable EF Core
        // persistence when a database is configured; otherwise the in-memory adapter
        // (seeded with the curated catalog) keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<IReadingRepository, EfReadingRepository>();
        else
            services.AddSingleton<IReadingRepository, InMemoryReadingRepository>();

        services.AddSingleton<IReadingContentProvider>(_ => JsonReadingContentProvider.FromEmbeddedResource());

        // i+1 comprehensibility gate (docs/development-guide.md/PROJECT-SPEC core principle): scores generated
        // passages/transcripts against a known-vocabulary baseline so content that drifts too far
        // outside the target CEFR level is caught before it reaches learners. Stateless, so a
        // singleton is shared by the Reading and Listening generators below.
        services.AddSingleton<IComprehensibilityScorer, ComprehensibilityScorer>();

        // Topic-scoped reading lessons are cached in PostgreSQL; on a cache miss the request path
        // lazy-fills via the free Gemini model (when configured) and caches the result. Falls back to
        // the deterministic Local stand-in only when no Gemini key is set (dev/tests/offline).
        if (contentLlmConfigured)
            services.AddSingleton<IReadingContentGenerator>(sp =>
                new LlmReadingContentGenerator(
                    ContentCompletion(sp),
                    sp.GetRequiredService<IComprehensibilityScorer>(),
                    sp.GetRequiredService<ILogger<LlmReadingContentGenerator>>()));
        else
            services.AddSingleton<IReadingContentGenerator, LocalReadingContentGenerator>();

        // Books library (Home → Books). Multi-section graded readers stored in PostgreSQL: the
        // catalog metadata is seeded and each section's text + ten comprehension questions are
        // generated lazily on first open and cached. Durable EF Core persistence when a database is
        // configured; otherwise the in-memory adapter (seeded with the curated library) keeps the
        // app runnable in dev/tests.
        if (hasDatabase)
        {
            services.AddScoped<IBookRepository, EfBookRepository>();
            services.AddScoped<IBookProgressStore, EfBookProgressStore>();
        }
        else
        {
            services.AddSingleton<IBookRepository, InMemoryBookRepository>();
            services.AddSingleton<IBookProgressStore, InMemoryBookProgressStore>();
        }

        // Book section content is cached in PostgreSQL; on a cache miss the request path lazy-fills via
        // Hermes when configured. The deterministic local generator also covers transient gateway
        // failures so a learner never gets stranded on a permanently loading book section.
        services.AddSingleton<IBookContentGenerator>(sp =>
        {
            IBookContentGenerator primary = contentLlmConfigured
                ? new LlmBookContentGenerator(ContentCompletion(sp))
                : new LocalBookContentGenerator();
            return new FallbackBookContentGenerator(primary, new LocalBookContentGenerator());
        });

        // Licensed images (docs/development-guide.md rule 12): Unsplash and/or Pexels when a key is configured,
        // plus the keyless Wikimedia Commons provider as an always-available fallback (Commons hosts
        // only CC/public-domain media). Chained by CompositeImageService (keyed providers first,
        // Wikimedia tops up a topic's gallery), so a gallery still fills with no key at all and even
        // if one provider rate-limits. Resolved URLs/bytes are persisted by the caller (book covers,
        // topic galleries). Wikimedia asks for a descriptive User-Agent, so each client sets one.
        var images = configuration.GetSection(ImageOptions.SectionName).Get<ImageOptions>()
                     ?? new ImageOptions();
        services.AddSingleton(images);
        var moderation = configuration.GetSection(ImageModerationOptions.SectionName)
                             .Get<ImageModerationOptions>() ?? new ImageModerationOptions();
        services.AddSingleton(moderation);
        services.AddSingleton(new ImageSafetyServingOptions
        {
            RequireSafetyApproval = moderation.RequireSafetyApproval,
        });
        if (moderation.Enabled)
        {
            services.AddHttpClient<HttpImageSafetyClassifier>(client =>
            {
                client.BaseAddress = new Uri(moderation.BaseUrl);
                client.Timeout = TimeSpan.FromSeconds(Math.Clamp(moderation.TimeoutSeconds, 2, 120));
            });
            services.AddTransient<IImageSafetyClassifier>(sp =>
                sp.GetRequiredService<HttpImageSafetyClassifier>());
        }
        else
        {
            services.AddSingleton<IImageSafetyClassifier, DisabledImageSafetyClassifier>();
        }
        if (images.HasUnsplash)
            services.AddHttpClient<UnsplashImageService>();
        if (images.HasPexels)
            services.AddHttpClient<PexelsImageService>();
        services.AddHttpClient<WikimediaImageService>(c =>
            c.DefaultRequestHeaders.UserAgent.ParseAdd("EnglishAI.uz/1.0 (https://englishai.uz; learning platform)"));

        services.AddTransient<IImageService>(sp =>
        {
            var providers = new List<IImageService>();
            if (images.HasUnsplash) providers.Add(sp.GetRequiredService<UnsplashImageService>());
            if (images.HasPexels) providers.Add(sp.GetRequiredService<PexelsImageService>());
            providers.Add(sp.GetRequiredService<WikimediaImageService>());
            IImageService composite = new CompositeImageService(providers);
            if (!moderation.Enabled)
                return composite;
            return new SafetyCheckedImageService(
                composite,
                sp.GetRequiredService<IImageSafetyClassifier>(),
                moderation,
                sp.GetRequiredService<ILogger<SafetyCheckedImageService>>());
        });

        // Topic thumbnails downloaded once per topic and stored as bytea (rule 12): EF Core /
        // PostgreSQL persistence when a database is configured, in-memory otherwise. The backfill
        // job (triggered by the admin endpoint, run on Hangfire) fills one image per topic; the
        // image is served back from the database by GetTopicImageQuery.
        if (hasDatabase)
            services.AddScoped<ITopicImageStore, EfTopicImageStore>();
        else
            services.AddSingleton<ITopicImageStore, InMemoryTopicImageStore>();
        services.AddScoped<WordImageBackfillJob>();
        services.AddScoped<TopicImageBackfillJob>();
        services.AddScoped<ImageSafetyAuditJob>();
        services.AddScoped<IVocabularyImageReplacementService, VocabularyImageReplacementService>();

        // Faza 4 listening (leveled audio synthesized by Azure TTS + comprehension quiz).
        // Durable EF Core persistence when a database is configured; otherwise the in-memory
        // adapter (seeded with the curated catalog) keeps the app runnable in dev/tests. The
        // audio cache keeps each clip's synthesis to one Azure call (rule 10).
        if (hasDatabase)
            services.AddScoped<IListeningRepository, EfListeningRepository>();
        else
            services.AddSingleton<IListeningRepository, InMemoryListeningRepository>();

        services.AddSingleton<IListeningContentProvider>(_ => JsonListeningContentProvider.FromEmbeddedResource());

        // Synthesized clips are cached so each is produced by Azure TTS at most once (rule 10).
        // Durable (PostgreSQL) when a database is configured - survives restarts; otherwise the
        // in-memory cache keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<IListeningAudioCache, EfListeningAudioCache>();
        else
            services.AddSingleton<IListeningAudioCache, InMemoryListeningAudioCache>();

        // Topic-scoped listening exercises are cached in PostgreSQL; on a cache miss the request path
        // lazy-fills via the free Gemini model (when configured) and caches the result. The transcript
        // is then synthesized to audio by Azure TTS and cached behind the audio endpoint. Falls back to
        // the deterministic Local stand-in only when no Gemini key is set (dev/tests/offline).
        if (contentLlmConfigured)
            services.AddSingleton<IListeningContentGenerator>(sp =>
                new LlmListeningContentGenerator(
                    ContentCompletion(sp),
                    sp.GetRequiredService<IComprehensibilityScorer>(),
                    sp.GetRequiredService<ILogger<LlmListeningContentGenerator>>()));
        else
            services.AddSingleton<IListeningContentGenerator, LocalListeningContentGenerator>();

        // Faza 6 grammar (topic-scoped 5-step lessons, PROJECT-SPEC G.2). Each topic's grammar
        // focus is taught in the topic's own context, generated lazily on first open and cached.
        // Durable EF Core persistence when a database is configured; otherwise the in-memory
        // adapter (lazily filled per topic) keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<IGrammarRepository, EfGrammarRepository>();
        else
            services.AddSingleton<IGrammarRepository, InMemoryGrammarRepository>();

        services.AddSingleton<IGrammarContentProvider>(_ => JsonGrammarContentProvider.FromEmbeddedResource());

        // Topic-scoped grammar lessons are cached in PostgreSQL; on a cache miss the request path
        // lazy-fills via the free Gemini model (when configured) and caches the result. Falls back to
        // the deterministic Local stand-in only when no Gemini key is set (dev/tests/offline).
        if (contentLlmConfigured)
            services.AddSingleton<IGrammarContentGenerator>(sp =>
                new LlmGrammarContentGenerator(
                    ContentCompletion(sp), sp.GetRequiredService<IGrammarContentProvider>()));
        else
            services.AddSingleton<IGrammarContentGenerator, LocalGrammarContentGenerator>();

        // Global English-learning assistant. Prefer the internal Hermes gateway, but keep the feature
        // available through the existing free-provider chain when that local gateway is stopped.
        // HermesLearningAssistant still owns both answer generation and Uzbek quality verification.
        if (contentLlmConfigured)
            services.AddSingleton<ILearningAssistant>(sp => new HermesLearningAssistant(
                ContentCompletion(sp)));
        else
            services.AddSingleton<ILearningAssistant, NoOpLearningAssistant>();

        if (contentLlmConfigured)
            services.AddSingleton<IProjectAssistant>(sp => new HermesProjectAssistant(
                ContentCompletion(sp),
                sp.GetRequiredService<ProjectAssistantOptions>(),
                sp.GetRequiredService<ILogger<HermesProjectAssistant>>()));
        else
            services.AddSingleton<IProjectAssistant, NoOpProjectAssistant>();

        services.AddSingleton<LocalContextualAssistant>();
        var contextualAssistant = configuration.GetSection(ContextualAssistantOptions.SectionName).Get<ContextualAssistantOptions>()
                                  ?? new ContextualAssistantOptions();
        services.AddSingleton(contextualAssistant);
        if (contentLlmConfigured)
            services.AddSingleton<IContextualAssistant>(sp => new LlmContextualAssistant(ContentCompletion(sp)));
        else
            services.AddSingleton<IContextualAssistant>(sp => sp.GetRequiredService<LocalContextualAssistant>());
        services.AddSingleton<IContextualAssistantCoordinator, ContextualAssistantCoordinator>();
        services.AddScoped<IAssistantKnowledgeRetriever, PlatformAssistantKnowledgeRetriever>();
        services.AddSingleton(TimeProvider.System);
        if (hasDatabase)
            services.AddScoped<Application.Assistant.Sessions.IAssistantSessionRepository, EfAssistantSessionRepository>();
        else
            services.AddSingleton<Application.Assistant.Sessions.IAssistantSessionRepository, InMemoryAssistantSessionRepository>();
        services.AddScoped<Application.Assistant.Sessions.IAssistantSessionService, Application.Assistant.Sessions.AssistantSessionService>();
        services.AddScoped<AssistantSessionCleanupJob>();

        // On-demand English→Uzbek sentence translation (the "click/hover a sentence for its meaning"
        // feature). Cache-first so each distinct sentence is translated at most once (rules 10, 11).
        services.AddSingleton<ITranslationCache, InMemoryTranslationCache>();
        var videoExplain = configuration.GetSection(VideoExplainOptions.SectionName).Get<VideoExplainOptions>()
                           ?? new VideoExplainOptions();
        services.AddSingleton(videoExplain);
        if (contentLlmConfigured)
            services.AddSingleton<ITextTranslator>(sp => new ResilientTextTranslator(
                new LlmTextTranslator(ContentCompletion(sp)),
                sp.GetRequiredService<TimeProvider>(),
                sp.GetRequiredService<ILogger<ResilientTextTranslator>>()));
        else
            services.AddSingleton<ITextTranslator, LocalTextTranslator>();

        // Video-lesson "explain in Uzbek" chat panel - free-text Q&A about a real transcript
        // line/passage (rule 11's translation exception, widened to synthesized explanation; see
        // IChatExplainer). No cache (each question is distinct); honest no-op without an LLM key.
        if (contentLlmConfigured)
            services.AddSingleton<IChatExplainer>(sp => new LlmChatExplainer(
                ContentCompletion(sp), sp.GetRequiredService<ILogger<LlmChatExplainer>>()));
        else
            services.AddSingleton<IChatExplainer, LocalChatExplainer>();
        services.AddSingleton<IVideoExplainCoordinator, VideoExplainCoordinator>();
        services.AddSingleton<IVideoQuizStore, VideoQuizStore>();
        services.AddSingleton<IVideoQuizGenerator>(sp => new LlmVideoQuizGenerator(
            contentLlmConfigured ? ContentCompletion(sp) : null, sp.GetRequiredService<IVideoExplainCache>()));

        // Faza 6 writing (topic-scoped prompts + 4-dimension AI assessment, PROJECT-SPEC G.3).
        // Each topic's writing task is generated lazily on first open and cached. Durable EF Core
        // persistence when a database is configured; otherwise the in-memory adapter (lazily filled
        // per topic) keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<IWritingTaskRepository, EfWritingTaskRepository>();
        else
            services.AddSingleton<IWritingTaskRepository, InMemoryWritingTaskRepository>();

        services.AddSingleton<IWritingContentProvider>(_ => JsonWritingContentProvider.FromEmbeddedResource());

        // Topic-scoped writing prompts are generated per learning-spine topic and cached (rules
        // 10, 11). The deterministic local generator also covers transient gateway failures so a
        // learner never gets stranded on a permanently pending writing page.
        services.AddSingleton<IWritingPromptGenerator>(sp =>
        {
            IWritingPromptGenerator primary = contentLlmConfigured
                ? new HermesWritingPromptGenerator(ContentCompletion(sp))
                : new LocalWritingPromptGenerator();
            return new FallbackWritingPromptGenerator(primary, new LocalWritingPromptGenerator());
        });

        // AI remains the primary topic assessor. A bounded resilient wrapper falls back to the
        // explicitly-labelled local assessment when the provider times out or fails, so a learner's
        // submission never waits ~100 seconds only to end in HTTP 500.
        services.AddSingleton<LocalWritingAssessor>();
        if (contentLlmConfigured)
        {
            services.AddSingleton<ITopicWritingAssessor>(sp => new ResilientWritingAssessor(
                new HermesWritingAssessor(ContentCompletion(sp)),
                sp.GetRequiredService<LocalWritingAssessor>(),
                sp.GetRequiredService<ILogger<ResilientWritingAssessor>>()));
            // Placement also needs the configured examiner. Its bounded fallback keeps
            // a provider outage from stranding an otherwise-completed six-skill test.
            services.AddSingleton<IWritingAssessor>(sp => sp.GetRequiredService<ITopicWritingAssessor>());
        }
        else
        {
            services.AddSingleton<ITopicWritingAssessor>(sp => sp.GetRequiredService<LocalWritingAssessor>());
            services.AddSingleton<IWritingAssessor>(sp => sp.GetRequiredService<LocalWritingAssessor>());
        }

        // Faza 7 retention/growth (PROJECT-SPEC Qism I). Feature-flag experiments use durable
        // EF Core persistence when a database is configured; otherwise the in-memory adapter
        // (seeded with the starter experiments) keeps the app runnable in dev/tests. The
        // daily win-back sweep is a thin Hangfire entry point over the Application handler.
        if (hasDatabase)
            services.AddScoped<IFeatureFlagRepository, EfFeatureFlagRepository>();
        else
            services.AddSingleton<IFeatureFlagRepository, InMemoryFeatureFlagRepository>();

        services.AddScoped<RetentionSweepJob>();

        // Live "Musobaqa" (competition) game. Durable EF Core persistence when a database is
        // configured; otherwise the in-memory adapter keeps the app runnable in dev/tests.
        if (hasDatabase)
            services.AddScoped<ICompetitionRepository, EfCompetitionRepository>();
        else
            services.AddSingleton<ICompetitionRepository, InMemoryCompetitionRepository>();

        // Content backfill (docs/development-guide.md rule 10): eagerly generate every module's content for every
        // topic and cache it in the database, so users are always served from the database and the
        // LLM is never on the request path. The bulk backfillers reuse each module's free-Gemini
        // generator (same prompt + parser); only writing reuses the Claude generator. The job is
        // module-agnostic. Scoped so the backfillers can resolve the (scoped) EF repositories.
        services.AddScoped<IContentBackfiller, VocabularyContentBackfiller>();
        services.AddScoped<IContentBackfiller, ReadingContentBackfiller>();
        services.AddScoped<IContentBackfiller, GrammarContentBackfiller>();
        services.AddScoped<IContentBackfiller, ListeningContentBackfiller>();
        services.AddScoped<IContentBackfiller, WritingContentBackfiller>();

        // The backfill runner routes every request (bulk modules AND writing) through the Hermes
        // gateway - the only LLM backend left - so the whole catalogue fills at zero cost against a
        // self-hosted model (docs/development-guide.md rule 10). The Claude batch path stays unused by the backfill.
        services.AddSingleton<IContentBatchClient>(sp => new RoutingContentBatchClient(
            sp.GetRequiredService<ResilientHermesGatewayLlmCompletion>(),
            sp.GetRequiredService<ILogger<RoutingContentBatchClient>>()));

        services.AddScoped<ContentBackfillJob>();

        var backfillLlm = configuration.GetSection(BackfillLlmOptions.SectionName).Get<BackfillLlmOptions>()
                          ?? new BackfillLlmOptions();
        services.AddSingleton(backfillLlm);
        services.AddHttpClient("backfill-edubase", client =>
            client.Timeout = TimeSpan.FromSeconds(Math.Clamp(backfillLlm.RequestTimeoutSeconds, 30, 600)));
        if (hasDatabase)
        {
            services.AddSingleton<EdubaseBackfillClient>();
            services.AddScoped<CurriculumBackfillService>();
            services.AddScoped<CurriculumPublisher>();
        }

        return services;
    }

    /// <summary>
    /// The FCM project id comes from explicit config when set, otherwise from the <c>project_id</c>
    /// inside the service-account JSON (which always carries it), so operators only need to drop in
    /// the key file.
    /// </summary>
    private static string ResolveFcmProjectId(FcmOptions options, string serviceAccountJson)
    {
        if (!string.IsNullOrWhiteSpace(options.ProjectId))
            return options.ProjectId!;

        using var doc = System.Text.Json.JsonDocument.Parse(serviceAccountJson);
        if (doc.RootElement.TryGetProperty("project_id", out var pid) && pid.GetString() is { Length: > 0 } id)
            return id;

        throw new InvalidOperationException(
            "FCM is configured but no project id was found (set Fcm:ProjectId or ensure the service account JSON has project_id).");
    }
}
