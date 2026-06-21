using Application.Ai;
using Application.Common;
using Application.Subscription.Ports;
using Domain.Subscription;
using FluentAssertions;
using Infrastructure.Ai;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.AspNetCore.Routing.Patterns;
using Web.Middleware;
using Xunit;

namespace Integration.Tests.Ai;

public sealed class AiRequestContextResolverTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 1, 0, 0, 0, TimeSpan.Zero);

    [Fact]
    public void Production_shape_can_validate_singleton_ai_services_without_capturing_scoped_dependencies()
    {
        var services = new ServiceCollection();
        services.AddSingleton(TimeProvider.System);
        services.AddScoped<ISubscriptionRepository, TrackingSubscriptionRepository>();
        services.AddSingleton<IAiRequestContextResolver, AiRequestContextResolver>();
        services.AddSingleton<IAiFeatureScope, AiFeatureScope>();

        using var provider = services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

        provider.GetRequiredService<IAiFeatureScope>().Should().BeOfType<AiFeatureScope>();
    }

    [Fact]
    public async Task Parallel_resolves_use_distinct_scoped_subscription_repositories()
    {
        lock (TrackingSubscriptionRepository.InstanceIds)
            TrackingSubscriptionRepository.InstanceIds.Clear();
        var services = new ServiceCollection();
        services.AddScoped<ISubscriptionRepository, TrackingSubscriptionRepository>();
        using var provider = services.BuildServiceProvider();
        var resolver = new AiRequestContextResolver(
            provider.GetRequiredService<IServiceScopeFactory>(), new FixedTimeProvider(Now));
        var learnerId = Guid.NewGuid();

        var results = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            resolver.ResolveAsync(AiFeature.WritingAssessment, learnerId.ToString(), CancellationToken.None)));

        results.Should().OnlyContain(result => result.Tier == AiSubscriptionTier.Pro);
        Guid[] instanceIds;
        lock (TrackingSubscriptionRepository.InstanceIds)
            instanceIds = TrackingSubscriptionRepository.InstanceIds.ToArray();
        instanceIds.Should().OnlyHaveUniqueItems();
        instanceIds.Should().HaveCount(8);
    }

    [Theory]
    [InlineData("/api/assistant/context", AiFeature.Assistant)]
    [InlineData("/api/translate", AiFeature.Translation)]
    [InlineData("/api/speaking/assess", AiFeature.SpeakingTutor)]
    [InlineData("/api/video/explain", AiFeature.VideoExplain)]
    public async Task Middleware_sets_authenticated_caller_and_endpoint_attribution(
        string path,
        AiFeature expectedFeature)
    {
        var learnerId = Guid.NewGuid();
        AiAdmissionContext.State? captured = null;
        var middleware = new AiRequestContextMiddleware(_ =>
        {
            captured = AiAdmissionContext.Current;
            return Task.CompletedTask;
        });
        var http = new DefaultHttpContext();
        http.Request.Path = path;
        var contexts = new StubRequestContextResolver(AiSubscriptionTier.Pro);

        await middleware.InvokeAsync(http, new StubCurrentUser(learnerId), contexts);

        captured.Should().NotBeNull();
        captured!.CallerKey.Should().Be(learnerId.ToString());
        captured.Tier.Should().Be(AiSubscriptionTier.Pro);
        captured.Feature.Should().Be(expectedFeature);
        captured.RequestPath.Should().Be(path);
        AiAdmissionContext.Current.Should().BeSameAs(AiAdmissionContext.State.Anonymous);
    }

    [Fact]
    public async Task Middleware_uses_route_template_to_bound_endpoint_cardinality()
    {
        AiAdmissionContext.State? captured = null;
        var middleware = new AiRequestContextMiddleware(_ =>
        {
            captured = AiAdmissionContext.Current;
            return Task.CompletedTask;
        });
        var http = new DefaultHttpContext();
        http.Request.Path = "/api/speaking/word/make";
        http.SetEndpoint(new RouteEndpoint(
            _ => Task.CompletedTask,
            RoutePatternFactory.Parse("/api/speaking/word/{word}"),
            0,
            EndpointMetadataCollection.Empty,
            "speaking-word"));

        await middleware.InvokeAsync(
            http,
            new StubCurrentUser(Guid.NewGuid()),
            new StubRequestContextResolver(AiSubscriptionTier.Free));

        captured!.RequestPath.Should().Be("/api/speaking/word/{word}");
    }

    [Fact]
    public async Task Middleware_buckets_anonymous_ai_by_client_ip()
    {
        AiAdmissionContext.State? captured = null;
        var middleware = new AiRequestContextMiddleware(_ =>
        {
            captured = AiAdmissionContext.Current;
            return Task.CompletedTask;
        });
        var http = new DefaultHttpContext();
        http.Connection.RemoteIpAddress = System.Net.IPAddress.Parse("203.0.113.42");
        http.Request.Path = "/api/assistant/project";

        await middleware.InvokeAsync(
            http,
            new StubCurrentUser(null),
            new StubRequestContextResolver(AiSubscriptionTier.Free));

        captured!.CallerKey.Should().Be("ip:203.0.113.42");
        captured.Feature.Should().Be(AiFeature.Assistant);
    }

    [Fact]
    public async Task Nested_feature_scope_preserves_anonymous_request_caller_key()
    {
        var resolver = new StubRequestContextResolver(AiSubscriptionTier.Free);
        var featureScope = new AiFeatureScope(resolver);

        using var requestScope = AiAdmissionContext.Push(new(
            "ip:203.0.113.42",
            AiSubscriptionTier.Free,
            AiFeature.Assistant,
            "/api/assistant/project"));
        using var nested = await featureScope.EnterAsync(AiFeature.Assistant, null, CancellationToken.None);

        AiAdmissionContext.Current.CallerKey.Should().Be("ip:203.0.113.42");
        AiAdmissionContext.Current.RequestPath.Should().Be("/api/assistant/project");
    }

    private sealed class TrackingSubscriptionRepository : ISubscriptionRepository
    {
        public static List<Guid> InstanceIds { get; } = new();
        private readonly Guid _instanceId = Guid.NewGuid();

        public TrackingSubscriptionRepository()
        {
            lock (InstanceIds) InstanceIds.Add(_instanceId);
        }

        public Task<Domain.Subscription.Subscription?> GetByLearnerIdAsync(
            Guid learnerId, CancellationToken cancellationToken)
        {
            var subscription = Domain.Subscription.Subscription.CreateFree(learnerId, Now);
            subscription.Activate(SubscriptionPlan.Monthly, Now);
            return Task.FromResult<Domain.Subscription.Subscription?>(subscription);
        }

        public Task<IReadOnlyDictionary<Guid, Domain.Subscription.Subscription>> GetManyByLearnerIdsAsync(
            IReadOnlyCollection<Guid> learnerIds, CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyDictionary<Guid, Domain.Subscription.Subscription>>(
                new Dictionary<Guid, Domain.Subscription.Subscription>());

        public Task<IReadOnlyList<Domain.Subscription.Subscription>> GetPaidAsync(CancellationToken cancellationToken) =>
            Task.FromResult<IReadOnlyList<Domain.Subscription.Subscription>>(Array.Empty<Domain.Subscription.Subscription>());

        public Task SaveAsync(Domain.Subscription.Subscription subscription, CancellationToken cancellationToken) =>
            Task.CompletedTask;
    }

    private sealed class FixedTimeProvider(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed record StubCurrentUser(Guid? LearnerId) : ICurrentUserAccessor;

    private sealed class StubRequestContextResolver(AiSubscriptionTier tier) : IAiRequestContextResolver
    {
        public Task<AiRequestContext> ResolveAsync(
            AiFeature feature,
            string? callerKey,
            CancellationToken cancellationToken) =>
            Task.FromResult(new AiRequestContext(callerKey ?? "anonymous", tier, feature));
    }
}
