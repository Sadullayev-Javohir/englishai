using Application.Ai;
using Application.Subscription.Ports;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure.Ai;

public sealed class AiRequestContextResolver(IServiceScopeFactory scopeFactory, TimeProvider clock) : IAiRequestContextResolver
{
    public async Task<AiRequestContext> ResolveAsync(AiFeature feature, string? callerKey, CancellationToken cancellationToken)
    {
        var normalized = string.IsNullOrWhiteSpace(callerKey) ? "anonymous" : callerKey.Trim();
        if (!Guid.TryParse(normalized, out var learnerId)) return new(normalized, AiSubscriptionTier.Free, feature);

        await using var scope = scopeFactory.CreateAsyncScope();
        var subscriptions = scope.ServiceProvider.GetRequiredService<ISubscriptionRepository>();
        var subscription = await subscriptions.GetByLearnerIdAsync(learnerId, cancellationToken);
        var tier = subscription?.IsPremiumActive(clock.GetUtcNow()) == true ? AiSubscriptionTier.Pro : AiSubscriptionTier.Free;
        return new(normalized, tier, feature);
    }
}
