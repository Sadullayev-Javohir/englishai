namespace Application.Ai;

public interface IAiRequestContextResolver
{
    Task<AiRequestContext> ResolveAsync(AiFeature feature, string? callerKey, CancellationToken cancellationToken);
}

public sealed record AiRequestContext(string CallerKey, AiSubscriptionTier Tier, AiFeature Feature);
