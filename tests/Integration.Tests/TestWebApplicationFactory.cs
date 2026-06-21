using Application.Assessment.Ports;
using Application.Assistant.Ports;
using Application.Common;
using Application.Gamification.Ports;
using Application.Books.Ports;
using Application.Grammar.Ports;
using Application.Listening.Ports;
using Application.Translation.Ports;
using Application.Video.Models;
using Application.Learning.Ports;
using Application.Reading.Ports;
using Application.Retention.Ports;
using Application.Speaking.Ports;
using Application.Subscription.Ports;
using Application.Video.Ports;
using Application.Vocabulary.Ports;
using Application.Writing.Ports;
using Infrastructure.Assessment;
using Infrastructure.Assistant;
using Infrastructure.Books;
using Infrastructure.Gamification;
using Infrastructure.Grammar;
using Infrastructure.Listening;
using Infrastructure.Speaking;
using Infrastructure.Translation;
using Infrastructure.Writing;
using Infrastructure.Learning;
using Infrastructure.Reading;
using Infrastructure.Retention;
using Infrastructure.Subscription;
using Infrastructure.Video;
using Infrastructure.Vocabulary;
using Web.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Integration.Tests;

/// <summary>
/// Boots the Web app for end-to-end HTTP tests on the in-memory adapters, with no
/// database required (no Docker). The learner-profile and vocabulary repositories are
/// forced to in-memory so requests never reach Postgres, and Hangfire is disabled so no
/// background server runs during tests. Real-DB coverage lives in the Testcontainers
/// smoke test (<c>Category=Integration</c>).
///
/// Azure Speech / LLM keys (which a developer may have in user-secrets) are blanked here
/// so the config-gated Speech/LLM registrations always resolve to the deterministic
/// Local* adapters - otherwise these HTTP tests would hit real Azure with synthetic audio.
/// </summary>
public sealed class TestWebApplicationFactory : WebApplicationFactory<Program>
{
    static TestWebApplicationFactory()
    {
        // Force Infrastructure to register the in-memory adapters (hasDatabase=false) and
        // make DatabaseInitializer skip MigrateAsync, so these HTTP flow tests never touch
        // Postgres. Real-DB coverage lives in the Testcontainers tests (Category=Integration),
        // which build their own DbContext and ignore this connection string.
        //
        // This decision happens at AddInfrastructure() time (in Program.cs, before
        // ConfigureTestServices runs), so the override has to land in the configuration the
        // app builds from. UseSetting / ConfigureAppConfiguration both lose to appsettings.json
        // under the minimal-hosting model - the app re-adds appsettings after the factory's
        // callbacks, so the blank never took effect and the app kept booting against Postgres
        // (a 7s connect timeout → 500 on every request in CI, where no Postgres is running).
        //
        // An environment variable is added by CreateBuilder *after* appsettings.json, so it
        // wins. It must be a non-empty value (SetEnvironmentVariable deletes the variable when
        // given null/empty), so we use whitespace: hasDatabase uses IsNullOrWhiteSpace, which
        // still resolves whitespace to "no database".
        Environment.SetEnvironmentVariable("ConnectionStrings__Postgres", " ");
        Environment.SetEnvironmentVariable("Redis__ConnectionString", " ");

        // Disable the perimeter rate limiter for the HTTP flow tests: they fire many requests
        // in tight loops (and reuse a single client/IP), which would legitimately trip the
        // limit and make tests flaky. An env var wins over appsettings (same reason as above),
        // so this reliably turns it off regardless of config layering. Rate-limiting behaviour
        // itself is asserted by dedicated tests that opt back in.
        Environment.SetEnvironmentVariable("RateLimiting__Enabled", "false");
        Environment.SetEnvironmentVariable("Operations__BotToken", "integration-operations-token");
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseSetting("Hangfire:Enabled", "false");

        builder.ConfigureTestServices(services =>
        {
            // These HTTP flow tests target the module endpoints, not the Google sign-in flow
            // (covered by unit tests). Drop the global "must be authenticated" fallback so the
            // tests can call endpoints anonymously; with no authenticated user, the learner
            // ownership behavior also no-ops (it only enforces when a JWT sub is present).
            services.PostConfigure<AuthorizationOptions>(options => options.FallbackPolicy = null);

            services.RemoveAll<MobileGoogleSignInStore>();
            services.AddSingleton(new MobileGoogleSignInStore(null));

            services.RemoveAll<ILearnerProfileRepository>();
            services.AddSingleton<ILearnerProfileRepository, InMemoryLearnerProfileRepository>();

            services.RemoveAll<IVocabularyRepository>();
            services.AddSingleton<IVocabularyRepository, InMemoryVocabularyRepository>();

            services.RemoveAll<IVideoRepository>();
            services.AddSingleton<IVideoRepository, InMemoryVideoRepository>();

            // The real transcript provider scrapes YouTube; replace it with a no-op so HTTP
            // tests never make outbound calls (seed lessons have empty transcripts, which would
            // otherwise trigger a lazy fetch). Parsing is covered by YouTubeTranscriptProviderTests.
            services.RemoveAll<IVideoTranscriptProvider>();
            services.AddSingleton<IVideoTranscriptProvider, NoOpTranscriptProvider>();

            services.RemoveAll<IReadingRepository>();
            services.AddSingleton<IReadingRepository, InMemoryReadingRepository>();

            services.RemoveAll<IGrammarRepository>();
            services.AddSingleton<IGrammarRepository, InMemoryGrammarRepository>();

            services.RemoveAll<IWritingTaskRepository>();
            services.AddSingleton<IWritingTaskRepository, InMemoryWritingTaskRepository>();

            services.RemoveAll<ISubscriptionRepository>();
            services.AddSingleton<ISubscriptionRepository, InMemorySubscriptionRepository>();

            services.RemoveAll<IPaymentRepository>();
            services.AddSingleton<IPaymentRepository, InMemoryPaymentRepository>();

            // Production ships with online payments OFF (only the free plan is sold until a real
            // Click/Payme gateway is configured). The purchase-flow tests exercise the full
            // checkout machinery, so enable it here; the disabled path is covered by a unit test
            // and a dedicated override test.
            services.RemoveAll<IPaymentAvailability>();
            services.AddSingleton<IPaymentAvailability>(new ConfigPaymentAvailability(paymentsEnabled: true));

            services.RemoveAll<IFeatureFlagRepository>();
            services.AddSingleton<IFeatureFlagRepository, InMemoryFeatureFlagRepository>();

            // Force the in-memory gamification/usage adapters. appsettings.Development.json points
            // Redis at localhost:6381, so Infrastructure wires the Redis adapters at registration
            // time (redis.IsConfigured) even though no Redis runs in CI/tests. Like the DB/Speech
            // overrides above, blanking config later cannot undo that - replace the consumers here so
            // IConnectionMultiplexer is never resolved. Without this every entitlement/gamification
            // check times out connecting to Redis (5s) and 500s - green locally only because a dev
            // Redis happens to answer on 6381, red in CI where none exists.
            services.RemoveAll<IGamificationStore>();
            services.AddSingleton<IGamificationStore, InMemoryGamificationStore>();

            services.RemoveAll<IUsageCounter>();
            services.AddSingleton<IUsageCounter, InMemoryUsageCounter>();

            // Same reason: when Redis is "configured" the durable placement session store is wired
            // and would resolve IConnectionMultiplexer. Force the in-memory session store so the
            // placement flow tests never touch Redis.
            services.RemoveAll<IPlacementSessionStore>();
            services.AddSingleton<IPlacementSessionStore, InMemoryPlacementSessionStore>();

            services.RemoveAll<IPlacementSpeakingAssessor>();
            services.AddSingleton<IPlacementSpeakingAssessor, DeterministicPlacementSpeakingAssessor>();

            // Same reason: the leaderboard/points feature's leaderboard store is also wired from
            // Redis when "configured", and every module-completion flow now resolves it via
            // PointsService. Force the in-memory adapter so these flows never touch Redis.
            services.RemoveAll<ILeaderboardStore>();
            services.AddSingleton<ILeaderboardStore, InMemoryLeaderboardStore>();

            // Force the deterministic Local* Speech/LLM adapters even when a developer has
            // real Azure/LLM keys in user-secrets. Infrastructure picks Azure at registration
            // time from config, so blanking config later cannot undo it - replace the
            // services here instead. Synthetic test audio must never reach real Azure.
            services.RemoveAll<AzureSpeechOptions>();
            services.AddSingleton(new AzureSpeechOptions());

            // Same reason, for the Reading/Grammar lazy-fill content generators: a developer's real
            // EnglishAI-gateway key in user-secrets makes "contentLlmConfigured" true at Infrastructure
            // registration time, wiring the Llm-backed generator instead of the deterministic Local one
            // these HTTP flow tests expect (docs/development-guide.md rule 7 - never call real external APIs from
            // tests). Registration closes over the config captured at that moment, so - as with
            // Speech/Claude above - the fix is to replace the resolved generator here, not to blank the
            // options afterward.
            services.RemoveAll<IReadingContentGenerator>();
            services.AddSingleton<IReadingContentGenerator, LocalReadingContentGenerator>();

            services.RemoveAll<IGrammarContentGenerator>();
            services.AddSingleton<IGrammarContentGenerator, LocalGrammarContentGenerator>();

            services.RemoveAll<IVocabularyPassageGenerator>();
            services.AddSingleton<IVocabularyPassageGenerator, LocalVocabularyPassageGenerator>();

            services.RemoveAll<IWordUsageAssessor>();
            services.AddSingleton<IWordUsageAssessor, LocalWordUsageAssessor>();

            services.RemoveAll<IListeningContentGenerator>();
            services.AddSingleton<IListeningContentGenerator, LocalListeningContentGenerator>();

            services.RemoveAll<IBookContentGenerator>();
            services.AddSingleton<IBookContentGenerator, LocalBookContentGenerator>();

            services.RemoveAll<ICefrVideoLeveler>();
            services.AddSingleton<ICefrVideoLeveler, LocalCefrVideoLeveler>();

            services.RemoveAll<IVideoTranscriptTranslator>();
            services.AddSingleton<IVideoTranscriptTranslator, LocalTranscriptTranslator>();

            services.RemoveAll<IChatExplainer>();
            services.AddSingleton<IChatExplainer, LocalChatExplainer>();

            services.RemoveAll<ITextTranslator>();
            services.AddSingleton<ITextTranslator, LocalTextTranslator>();

            services.RemoveAll<ILearningAssistant>();
            services.AddSingleton<ILearningAssistant, NoOpLearningAssistant>();

            services.RemoveAll<IProjectAssistant>();
            services.AddSingleton<IProjectAssistant, NoOpProjectAssistant>();

            services.RemoveAll<IContextualAssistant>();
            services.AddSingleton<IContextualAssistant, LocalContextualAssistant>();

            services.RemoveAll<ISpeechToTextService>();
            services.AddSingleton<ISpeechToTextService, LocalSpeechToTextService>();

            services.RemoveAll<IPronunciationAssessor>();
            services.AddSingleton<IPronunciationAssessor, LocalPronunciationAssessor>();

            services.RemoveAll<ITextToSpeechService>();
            services.AddSingleton<ITextToSpeechService, LocalTextToSpeechService>();

            services.RemoveAll<IConversationTutor>();
            services.AddSingleton<IConversationTutor, LocalConversationTutor>();

            services.RemoveAll<IIdeaCardGenerator>();
            services.AddSingleton<IIdeaCardGenerator, LocalIdeaCardGenerator>();

            services.RemoveAll<IRoleplayEvaluator>();
            services.AddSingleton<IRoleplayEvaluator, LocalRoleplayEvaluator>();

            services.RemoveAll<IWritingPromptGenerator>();
            services.AddSingleton<IWritingPromptGenerator, LocalWritingPromptGenerator>();

            services.RemoveAll<ITopicWritingAssessor>();
            services.RemoveAll<IWritingAssessor>();
            services.RemoveAll<LocalWritingAssessor>();
            services.AddSingleton<LocalWritingAssessor>();
            services.AddSingleton<ITopicWritingAssessor>(sp => sp.GetRequiredService<LocalWritingAssessor>());
            services.AddSingleton<IWritingAssessor>(sp => sp.GetRequiredService<LocalWritingAssessor>());
        });
    }

    /// <summary>Transcript provider that never fetches - keeps HTTP tests offline and deterministic.</summary>
    private sealed class NoOpTranscriptProvider : IVideoTranscriptProvider
    {
        public Task<TranscriptFetchResult> FetchAsync(
            string youTubeVideoId, CancellationToken cancellationToken = default) =>
            Task.FromResult(TranscriptFetchResult.ProviderUnavailable);
    }

    private sealed class DeterministicPlacementSpeakingAssessor : IPlacementSpeakingAssessor
    {
        public Task<PlacementSpeakingScore> AssessAsync(
            byte[] audioContent,
            Domain.Assessment.PlacementSpeakingTask task,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(new PlacementSpeakingScore(35, PlacementSpeakingOutcome.Scored));
    }
}
